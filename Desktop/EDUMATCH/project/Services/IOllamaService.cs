namespace EduMatch.Services;

public interface IOllamaService
{
    Task<string> ExplainRoadmapAsync(
        List<RoadmapPhase> phases, string subjectName, string studentName);

    /// <summary>
    /// Trả về (answer, contextToken).
    /// Lần đầu: contextToken = null → nhúng toàn bộ roadmap vào prompt.
    /// Lần sau: truyền contextToken vào → Ollama nhớ context, chỉ gửi câu hỏi mới.
    /// </summary>
    Task<(string Answer, string? ContextToken)> AnswerQuestionAsync(
        string question, string subjectName, List<RoadmapPhase> phases,
        string? contextToken = null);
}
