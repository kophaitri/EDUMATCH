using EduMatch.Services.ML;
using EduMatch.Services.RoadmapEngine;

namespace EduMatch.Services;

public class RoadmapService : IRoadmapService
{
    private readonly EduMatchDbContext _db;
    private readonly IRoadmapEngine _engine;
    private readonly MLModelService _mlModel;
    private readonly IOllamaService _ollama;

    public RoadmapService(
        EduMatchDbContext db,
        IRoadmapEngine engine,
        MLModelService mlModel,
        IOllamaService ollama)
    {
        _db      = db;
        _engine  = engine;
        _mlModel = mlModel;
        _ollama  = ollama;
    }

    // ── CreateTopicAssessmentsAsync ─────────────────────────────────────────
    public async Task CreateTopicAssessmentsAsync(ExamSubmission submission)
    {
        var full = await _db.ExamSubmissions
            .Include(s => s.Answers)
                .ThenInclude(a => a.Question)
            .Include(s => s.Exam)
                .ThenInclude(e => e.Session)
                    .ThenInclude(s => s != null ? s.Contract : null)
            .FirstOrDefaultAsync(s => s.Id == submission.Id);

        if (full is null) return;

        // Entry exams have SubjectId directly; session-linked exams get it via Session→Contract
        int subjectId = (full.Exam?.IsEntryExam == true)
            ? (full.Exam.SubjectId ?? 0)
            : (full.Exam?.Session?.Contract?.SubjectId ?? 0);

        var assessments = full.Answers
            .Where(a => !string.IsNullOrEmpty(a.Question.TopicTag))
            .GroupBy(a => a.Question.TopicTag!)
            .Select(g =>
            {
                var items = g.ToList();
                int score = items.Count == 0 ? 0
                    : (int)Math.Round((float)items.Count(a => a.IsCorrect) / items.Count * 100);
                return new TopicAssessment
                {
                    SubmissionId = submission.Id,
                    StudentId    = submission.StudentId,
                    SubjectId    = subjectId,
                    TopicName    = g.Key,
                    Score        = score,
                    TakenAt      = submission.SubmittedAt ?? DateTime.UtcNow
                };
            })
            .ToList();

        if (assessments.Count > 0)
        {
            _db.TopicAssessments.AddRange(assessments);
            await _db.SaveChangesAsync();
        }
    }

    // ── GenerateAsync ───────────────────────────────────────────────────────
    public async Task<LearningRoadmap> GenerateAsync(string studentId, int subjectId, string studentName)
    {
        var assessments = await _db.TopicAssessments
            .Where(a => a.StudentId == studentId && a.SubjectId == subjectId)
            .OrderByDescending(a => a.TakenAt)
            .ToListAsync();

        var profile = await _db.StudentProfiles
            .FirstOrDefaultAsync(p => p.UserId == studentId)
            ?? new StudentProfile { UserId = studentId };

        var subject = await _db.Subjects.FindAsync(subjectId)
            ?? new Subject { Name = "Môn học" };

        // ── Layer 1: Rule engine ──────────────────────────────────────────
        var roadmap = _engine.Generate(assessments, profile, subjectId);

        // ── Layer 2: ML.NET ───────────────────────────────────────────────
        if (_mlModel.IsModelReady())
        {
            float ageGroup = GetAgeGroup(profile);

            foreach (var phase in roadmap.Phases)
            {
                var a = assessments.FirstOrDefault(x => x.TopicName == phase.TopicName);
                if (a is not null)
                {
                    float predicted = _mlModel.PredictWeeks(
                        a.Score,
                        profile.WeeklyAvailableHours,
                        ageGroup,
                        profile.TotalSessionsCompleted ?? 0);

                    if (predicted >= 1f && predicted <= 24f)
                        phase.EstimatedWeeks = predicted;
                }
            }
            roadmap.GeneratedBy = "MLNet";
        }

        // ── Layer 3: Ollama ───────────────────────────────────────────────
        var explanation = await _ollama.ExplainRoadmapAsync(
            roadmap.Phases.ToList(), subject.Name, studentName);
        if (!string.IsNullOrEmpty(explanation))
            roadmap.LlmExplanation = explanation;

        // Deactivate previous active roadmaps for same student+subject
        await _db.LearningRoadmaps
            .Where(r => r.StudentId == studentId && r.SubjectId == subjectId && r.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.IsActive, false));

        roadmap.StudentId = studentId;
        _db.LearningRoadmaps.Add(roadmap);
        await _db.SaveChangesAsync();

        return roadmap;
    }

    // ── GetActiveAsync ──────────────────────────────────────────────────────
    public async Task<LearningRoadmap?> GetActiveAsync(string studentId, int subjectId)
        => await _db.LearningRoadmaps
            .Include(r => r.Phases.OrderBy(p => p.WeekNumber))
            .Include(r => r.Subject)
            .FirstOrDefaultAsync(r =>
                r.StudentId == studentId &&
                r.SubjectId == subjectId &&
                r.IsActive);

    // ── LogProgressAsync ────────────────────────────────────────────────────
    public async Task LogProgressAsync(LearningProgressLog log)
    {
        _db.LearningProgressLogs.Add(log);
        await _db.SaveChangesAsync();

        // Mark matching phase complete when score >= 80
        if (log.ScoreAfter >= 80)
        {
            var phase = await _db.RoadmapPhases
                .Where(p => !p.IsCompleted &&
                            p.TopicName == log.TopicName &&
                            p.Roadmap.StudentId == log.StudentId &&
                            p.Roadmap.SubjectId == log.SubjectId &&
                            p.Roadmap.IsActive)
                .FirstOrDefaultAsync();

            if (phase is not null)
            {
                phase.IsCompleted = true;
                await _db.SaveChangesAsync();
            }
        }

        // Retrain every 100 real logs
        var logCount = await _db.LearningProgressLogs.CountAsync();
        if (logCount >= 100 && logCount % 100 == 0)
        {
            _ = Task.Run(() => RetainWithRealDataAsync());
        }
    }

    // ── GetProgressHistoryAsync ─────────────────────────────────────────────
    public async Task<List<LearningProgressLog>> GetProgressHistoryAsync(string studentId, int subjectId)
        => await _db.LearningProgressLogs
            .Where(l => l.StudentId == studentId && l.SubjectId == subjectId)
            .OrderByDescending(l => l.LoggedAt)
            .ToListAsync();

    // ── Private helpers ─────────────────────────────────────────────────────
    private async Task RetainWithRealDataAsync()
    {
        var logs = await _db.LearningProgressLogs.ToListAsync();
        var data = logs.Select(log => new ProgressTrainingData
        {
            ScoreBefore      = log.ScoreBefore,
            HoursPerWeek     = log.HoursStudied,
            AgeGroup         = 2f,
            PreviousSessions = 0f,
            WeeksToImprove   = Math.Clamp(
                (float)(log.LoggedAt - log.CreatedAt).TotalDays / 7f, 1f, 24f)
        }).ToList();

        _mlModel.Train(data);
    }

    private static float GetAgeGroup(StudentProfile p)
    {
        int grade = p.GradeLevelId ?? 0;
        return grade <= 5 ? 1f : grade <= 9 ? 2f : 3f;
    }
}
