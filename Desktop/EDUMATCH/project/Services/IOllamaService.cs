namespace EduMatch.Services;

public interface IOllamaService
{
    Task<string> ExplainRoadmapAsync(
        List<RoadmapPhase> phases, string subjectName, string studentName);
    Task<string> AnswerQuestionAsync(
        string question, string subjectName, List<RoadmapPhase> phases);
}
