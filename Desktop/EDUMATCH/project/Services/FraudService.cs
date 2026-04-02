using System.Text.Json;

namespace EduMatch.Services;

public class FraudService : IFraudService
{
    private readonly EduMatchDbContext _db;

    public FraudService(EduMatchDbContext db)
    {
        _db = db;
    }

    public async Task<List<SubmissionFraudSummary>> GetFraudSummariesForTutorAsync(string tutorId)
    {
        return await _db.ExamSubmissions
            .Include(s => s.Exam)
                .ThenInclude(e => e.Session)
                    .ThenInclude(s => s.Contract)
            .Include(s => s.Student)
            .Include(s => s.SuspiciousScore)
            .Where(s => s.Exam.Session.Contract.TutorId == tutorId && s.SuspiciousScore != null)
            .OrderByDescending(s => s.SuspiciousScore!.TotalScore)
            .Select(s => new SubmissionFraudSummary
            {
                SubmissionId = s.Id,
                StudentName = s.Student.FullName,
                ExamTitle = s.Exam.Title,
                TotalScore = s.SuspiciousScore!.TotalScore,
                Level = s.SuspiciousScore.Level,
                CalculatedAt = s.SuspiciousScore.CalculatedAt
            })
            .ToListAsync();
    }

    public async Task<FraudDetailDto?> GetFraudDetailAsync(string tutorId, int submissionId)
    {
        var submission = await _db.ExamSubmissions
            .Include(s => s.Exam)
                .ThenInclude(e => e.Session)
                    .ThenInclude(s => s.Contract)
            .Include(s => s.Student)
            .Include(s => s.SuspiciousScore)
            .Include(s => s.BehaviorLogs)
            .FirstOrDefaultAsync(s => s.Id == submissionId
                && s.Exam.Session.Contract.TutorId == tutorId);

        if (submission?.SuspiciousScore == null)
            return null;

        var score = submission.SuspiciousScore;
        var reasons = new List<string>();
        try { reasons = JsonSerializer.Deserialize<List<string>>(score.ReasonsJson) ?? new(); }
        catch { }

        var eventCounts = submission.BehaviorLogs
            .GroupBy(l => l.EventType)
            .ToDictionary(g => g.Key, g => g.Count());

        return new FraudDetailDto
        {
            SubmissionId = submission.Id,
            StudentName = submission.Student.FullName,
            ExamTitle = submission.Exam.Title,
            TotalScore = score.TotalScore,
            PasteScore = score.PasteScore,
            TabScore = score.TabScore,
            TimeScore = score.TimeScore,
            StyleScore = score.StyleScore,
            Level = score.Level,
            Reasons = reasons,
            CalculatedAt = score.CalculatedAt,
            BehaviorEventCounts = eventCounts
        };
    }
}
