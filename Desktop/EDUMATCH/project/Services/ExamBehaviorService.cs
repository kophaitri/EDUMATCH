using EduMatch.Controllers;

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
}
