namespace EduMatch.Services;

public interface IRoadmapService
{
    Task CreateTopicAssessmentsAsync(ExamSubmission submission);
    Task<LearningRoadmap> GenerateAsync(string studentId, int subjectId, string studentName);
    Task<LearningRoadmap?> GetActiveAsync(string studentId, int subjectId);
    Task LogProgressAsync(LearningProgressLog log);
    Task<List<LearningProgressLog>> GetProgressHistoryAsync(string studentId, int subjectId);
}
