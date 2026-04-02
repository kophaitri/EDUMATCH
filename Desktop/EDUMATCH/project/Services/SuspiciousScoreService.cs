using System.Text.Json;

namespace EduMatch.Services;

public class SuspiciousScoreService : ISuspiciousScoreService
{
    private readonly EduMatchDbContext _db;
    private readonly WritingStyleAnalyzer _styleAnalyzer;

    public SuspiciousScoreService(EduMatchDbContext db, WritingStyleAnalyzer styleAnalyzer)
    {
        _db = db;
        _styleAnalyzer = styleAnalyzer;
    }

    public async Task<ExamSuspiciousScore> CalculateAndSaveAsync(int submissionId)
    {
        // 1. Load behavior logs
        var logs = await _db.ExamBehaviorLogs
            .Where(l => l.SubmissionId == submissionId)
            .ToListAsync();

        // 2. Load submission with answers
        var submission = await _db.ExamSubmissions
            .Include(s => s.Answers)
            .Include(s => s.Student)
            .FirstAsync(s => s.Id == submissionId);

        var reasons = new List<string>();

        // 3. PasteScore
        var pasteCount = logs.Count(l => l.EventType == "PASTE");
        var pasteScore = Math.Min(pasteCount * 30f, 60f);
        if (pasteCount > 0)
            reasons.Add($"Phát hiện {pasteCount} lần dán nội dung (paste)");

        // 4. TabScore
        var tabLeaveCount = logs.Count(l => l.EventType == "TAB_LEAVE");
        var tabScore = Math.Min(tabLeaveCount * 20f, 60f);
        if (tabLeaveCount > 0)
            reasons.Add($"Phát hiện {tabLeaveCount} lần rời khỏi tab bài thi");

        // 5. TimeScore
        float timeScore = 0f;
        var questionIds = submission.Answers.Select(a => a.QuestionId).Distinct().ToList();

        if (questionIds.Count > 0 && submission.SubmittedAt.HasValue)
        {
            // Calculate time per answer using behavior logs or submission timestamps
            var allSubmissionsForExam = await _db.ExamSubmissions
                .Where(s => s.ExamId == submission.ExamId && s.SubmittedAt != null)
                .Include(s => s.Answers)
                .ToListAsync();

            if (allSubmissionsForExam.Count > 1)
            {
                foreach (var qId in questionIds)
                {
                    // Average time across all submissions (using total exam time / question count as proxy)
                    var avgTimes = allSubmissionsForExam
                        .Where(s => s.SubmittedAt.HasValue)
                        .Select(s =>
                        {
                            var totalSeconds = (s.SubmittedAt!.Value - s.StartedAt).TotalSeconds;
                            var qCount = s.Answers.Count;
                            return qCount > 0 ? totalSeconds / qCount : 0;
                        })
                        .Where(t => t > 0)
                        .ToList();

                    if (avgTimes.Count == 0) continue;
                    var avgTime = avgTimes.Average();

                    var thisSubmissionTime = (submission.SubmittedAt!.Value - submission.StartedAt).TotalSeconds;
                    var thisQuestionTime = submission.Answers.Count > 0
                        ? thisSubmissionTime / submission.Answers.Count
                        : 0;

                    if (thisQuestionTime > 0 && thisQuestionTime < avgTime * 0.3)
                    {
                        timeScore += 25f;
                    }
                }
                timeScore = Math.Min(timeScore, 50f);
                if (timeScore > 0)
                    reasons.Add("Thời gian trả lời một số câu hỏi nhanh bất thường");
            }
        }

        // 6. StyleScore
        float styleScore = 0f;
        var writtenAnswers = string.Join(" ",
            submission.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.TextAnswer))
                .Select(a => a.TextAnswer!));

        if (!string.IsNullOrWhiteSpace(writtenAnswers))
        {
            var profile = await _db.StudentWritingProfiles
                .FirstOrDefaultAsync(p => p.StudentId == submission.StudentId);

            if (profile != null)
            {
                var divergence = _styleAnalyzer.Compare(writtenAnswers, profile);
                if (divergence > 0.6f)
                {
                    styleScore = 35f;
                    reasons.Add("Phong cách viết khác biệt đáng kể so với lịch sử bài làm");
                }
            }
        }

        // 7. TotalScore
        var totalScore = Math.Min(pasteScore + tabScore + timeScore + styleScore, 100f);

        // 8. Level
        var level = totalScore switch
        {
            <= 30 => "Clean",
            <= 60 => "Suspicious",
            _ => "Flagged"
        };

        // 9. Build ReasonsJson
        var reasonsJson = JsonSerializer.Serialize(reasons);

        // 10. Upsert
        var existing = await _db.ExamSuspiciousScores
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);

        if (existing != null)
        {
            existing.TotalScore = totalScore;
            existing.PasteScore = pasteScore;
            existing.TabScore = tabScore;
            existing.TimeScore = timeScore;
            existing.StyleScore = styleScore;
            existing.Level = level;
            existing.ReasonsJson = reasonsJson;
            existing.CalculatedAt = DateTime.UtcNow;
        }
        else
        {
            existing = new ExamSuspiciousScore
            {
                SubmissionId = submissionId,
                TotalScore = totalScore,
                PasteScore = pasteScore,
                TabScore = tabScore,
                TimeScore = timeScore,
                StyleScore = styleScore,
                Level = level,
                ReasonsJson = reasonsJson,
                CalculatedAt = DateTime.UtcNow
            };
            _db.ExamSuspiciousScores.Add(existing);
        }

        // 11. If Flagged, create FraudWarning
        if (level == "Flagged")
        {
            var warning = new FraudWarning
            {
                SubmissionId = submissionId,
                WarningType = "AI_ANTI_CHEAT",
                Details = $"Điểm nghi ngờ: {totalScore:F0}/100. " + string.Join("; ", reasons),
                DetectedAt = DateTime.UtcNow
            };
            _db.FraudWarnings.Add(warning);
        }

        // 12. Save
        await _db.SaveChangesAsync();
        return existing;
    }

    public async Task<ExamSuspiciousScore?> GetBySubmissionAsync(int submissionId)
    {
        return await _db.ExamSuspiciousScores
            .FirstOrDefaultAsync(s => s.SubmissionId == submissionId);
    }
}
