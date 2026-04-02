namespace EduMatch.Services;

public interface ISuspiciousScoreService
{
    Task<ExamSuspiciousScore> CalculateAndSaveAsync(int submissionId);
    Task<ExamSuspiciousScore?> GetBySubmissionAsync(int submissionId);
}
