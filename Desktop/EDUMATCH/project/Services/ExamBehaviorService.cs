using EduMatch.Controllers;
using EduMatch.Models.Enums;

namespace EduMatch.Services;

public class ExamBehaviorService : IExamBehaviorService
{
    private readonly EduMatchDbContext _db;

    public ExamBehaviorService(EduMatchDbContext db)
    {
        _db = db;
    }

    public async Task<(bool Success, string? Error)> LogBehaviorAsync(string studentId, BehaviorLogRequest request)
    {
        var submission = await _db.ExamSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId && s.StudentId == studentId);

        if (submission == null)
            return (false, "Submission không thuộc về bạn");

        var logs = request.Events.Select(e => new ExamBehaviorLog
        {
            SubmissionId = request.SubmissionId,
            EventType = e.Type,
            TimestampMs = e.Timestamp,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        _db.ExamBehaviorLogs.AddRange(logs);
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<TabSwitchResponse?> HandleTabSwitchAsync(string studentId, int submissionId)
    {
        var submission = await _db.ExamSubmissions
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.StudentId == studentId);

        if (submission == null) return null;

        // Log the TAB_LEAVE event
        _db.ExamBehaviorLogs.Add(new ExamBehaviorLog
        {
            SubmissionId = submissionId,
            EventType = "TAB_LEAVE",
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Count total TAB_LEAVE events for this submission
        var count = await _db.ExamBehaviorLogs
            .CountAsync(l => l.SubmissionId == submissionId && l.EventType == "TAB_LEAVE");

        string action;

        if (count == 1)
        {
            action = "warn";
        }
        else if (count == 2)
        {
            action = "notify_tutor";

            // Notify tutor immediately
            _db.Notifications.Add(new Notification
            {
                UserId = submission.Exam.TutorId,
                Type = NotificationType.System,
                Title = "⚠️ Cảnh báo gian lận — Chuyển tab lần 2",
                Message = $"Học sinh đã rời khỏi trang thi \"{submission.Exam.Title}\" 2 lần. Lần tiếp theo bài sẽ tự động bị nộp.",
                ActionUrl = $"/tutor/exam/{submission.ExamId}/submissions/{submission.Id}",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            });
            _db.FraudWarnings.Add(new FraudWarning
            {
                SubmissionId = submissionId,
                WarningType = "TAB_SWITCH",
                Details = $"Học sinh chuyển tab lần thứ {count} — đã thông báo giáo viên",
                DetectedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }
        else
        {
            // count >= 3: force submit with fraud
            action = "force_submit";

            submission.IsFlagged = true;
            _db.FraudWarnings.Add(new FraudWarning
            {
                SubmissionId = submissionId,
                WarningType = "TAB_SWITCH_FRAUD",
                Details = $"Bài thi tự động bị nộp điểm 0 do chuyển tab lần thứ {count}",
                DetectedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        return new TabSwitchResponse(count, action);
    }

    public async Task<bool> HandleScreenshotAsync(string studentId, int submissionId, string method)
    {
        var submission = await _db.ExamSubmissions
            .Include(s => s.Exam)
            .FirstOrDefaultAsync(s => s.Id == submissionId && s.StudentId == studentId);

        if (submission == null) return false;

        // Count previous screenshot events for this submission
        var existingCount = await _db.FraudWarnings
            .CountAsync(w => w.SubmissionId == submissionId && w.WarningType == "SCREENSHOT");

        // Log behavior event
        _db.ExamBehaviorLogs.Add(new ExamBehaviorLog
        {
            SubmissionId = submissionId,
            EventType = "SCREENSHOT_ATTEMPT",
            TimestampMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Meta = method,
            CreatedAt = DateTime.UtcNow
        });

        // Record fraud warning
        _db.FraudWarnings.Add(new FraudWarning
        {
            SubmissionId = submissionId,
            WarningType = "SCREENSHOT",
            Details = $"Học sinh chụp màn hình lần {existingCount + 1} (phương thức: {method})",
            DetectedAt = DateTime.UtcNow
        });

        // Notify tutor on first screenshot (or every time — notify every time)
        _db.Notifications.Add(new Notification
        {
            UserId = submission.Exam.TutorId,
            Type = NotificationType.System,
            Title = "📸 Cảnh báo: Học sinh chụp màn hình đề thi",
            Message = $"Học sinh đã chụp màn hình trong lúc làm bài \"{submission.Exam.Title}\" " +
                      $"(lần {existingCount + 1}, phương thức: {method}).",
            ActionUrl = $"/tutor/exam/{submission.ExamId}/submissions/{submission.Id}",
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return true;
    }
}
