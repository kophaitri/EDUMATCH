using System.Text;
using System.Text.Json;

namespace EduMatch.Services;

public class OllamaService : IOllamaService
{
    private readonly HttpClient _http;
    private readonly ILogger<OllamaService> _logger;

    public OllamaService(HttpClient http, ILogger<OllamaService> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<string> ExplainRoadmapAsync(
        List<RoadmapPhase> phases, string subjectName, string studentName)
    {
        var phaseList = string.Join("\n", phases.Select(p =>
            $"- Tuần {p.WeekNumber}: {p.TopicName} ({p.SessionsPerWeek} buổi/tuần, ưu tiên {p.Priority})"));

        var prompt =
            $"Bạn là trợ lý học tập thân thiện.\n" +
            $"Học viên {studentName} vừa nhận lộ trình học môn {subjectName}:\n" +
            $"{phaseList}\n" +
            $"Giải thích ngắn gọn 3-4 câu tại sao lộ trình được sắp xếp như vậy.\n" +
            $"Dùng ngôn ngữ thân thiện, động viên. Tiếng Việt, không bullet point.";

        var result = await AskOllamaAsync(prompt);
        return !string.IsNullOrEmpty(result)
            ? result
            : FallbackExplain(phases, subjectName, studentName);
    }

    public async Task<string> AnswerQuestionAsync(
        string question, string subjectName, List<RoadmapPhase> phases)
    {
        var topics = string.Join(", ", phases.Select(p => p.TopicName));

        var prompt =
            $"Bạn là trợ lý học tập môn {subjectName}.\n" +
            $"Các topic trong lộ trình: {topics}.\n" +
            $"Câu hỏi: {question}\n" +
            $"Trả lời ngắn gọn, chính xác bằng tiếng Việt.";

        var result = await AskOllamaAsync(prompt);
        return !string.IsNullOrEmpty(result)
            ? result
            : FallbackAnswer(question, subjectName, phases);
    }

    // ── Ollama call ────────────────────────────────────────────────────────────
    private async Task<string> AskOllamaAsync(string prompt)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                model  = "llama3.2",
                prompt,
                stream = false
            });

            var response = await _http.PostAsync(
                "/api/generate",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama trả về {Status}", response.StatusCode);
                return string.Empty;
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.TryGetProperty("response", out var val)
                ? val.GetString() ?? string.Empty
                : string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Ollama không khả dụng — dùng fallback");
            return string.Empty;
        }
    }

    // ── Rule-based fallbacks (khi Ollama không chạy) ──────────────────────────
    private static string FallbackExplain(
        List<RoadmapPhase> phases, string subjectName, string studentName)
    {
        var highCount   = phases.Count(p => p.Priority == "high");
        var totalWeeks  = (int)Math.Ceiling(phases.Sum(p => p.EstimatedWeeks));
        var firstTopic  = phases.OrderBy(p => p.WeekNumber).FirstOrDefault()?.TopicName ?? "kiến thức nền";

        return $"Chào {studentName}! Lộ trình học môn {subjectName} của bạn được thiết kế trong khoảng {totalWeeks} tuần. " +
               $"Hệ thống bắt đầu từ \"{firstTopic}\" — đây là nền tảng quan trọng nhất. " +
               (highCount > 0
                   ? $"Có {highCount} chủ đề được đánh dấu ưu tiên cao dựa trên kết quả kiểm tra đầu vào của bạn — hãy tập trung đặc biệt vào các phần này. "
                   : "") +
               "Hãy kiên trì theo lộ trình và ghi nhận tiến độ sau mỗi buổi học để AI có thể điều chỉnh phù hợp hơn!";
    }

    private static string FallbackAnswer(
        string question, string subjectName, List<RoadmapPhase> phases)
    {
        var q = question.ToLower();
        var topics = phases.Select(p => p.TopicName).ToList();

        // Tìm topic liên quan trong câu hỏi
        var matchedTopic = topics.FirstOrDefault(t =>
            q.Contains(t.ToLower()) ||
            t.ToLower().Split(' ').Any(w => w.Length > 3 && q.Contains(w)));

        if (matchedTopic is not null)
        {
            var phase = phases.First(p => p.TopicName == matchedTopic);
            return $"Chủ đề \"{matchedTopic}\" trong lộ trình {subjectName} của bạn được xếp vào tuần {phase.WeekNumber}, " +
                   $"với {phase.SessionsPerWeek} buổi/tuần và ưu tiên mức \"{PriorityLabel(phase.Priority)}\". " +
                   (phase.Note is not null ? phase.Note : "Hãy ôn tập đều đặn và làm bài kiểm tra thử để theo dõi tiến độ.");
        }

        if (q.Contains("bao lâu") || q.Contains("thời gian") || q.Contains("tuần"))
        {
            var totalWeeks = (int)Math.Ceiling(phases.Sum(p => p.EstimatedWeeks));
            return $"Lộ trình {subjectName} của bạn dự kiến hoàn thành trong khoảng {totalWeeks} tuần " +
                   $"với {phases.Count} giai đoạn học. Thời gian thực tế phụ thuộc vào số giờ học mỗi tuần của bạn.";
        }

        if (q.Contains("ưu tiên") || q.Contains("quan trọng") || q.Contains("tập trung"))
        {
            var highPhases = phases.Where(p => p.Priority == "high").Select(p => p.TopicName).ToList();
            return highPhases.Any()
                ? $"Các chủ đề cần ưu tiên cao trong môn {subjectName}: {string.Join(", ", highPhases)}. " +
                  "Đây là những phần bạn còn yếu nhất theo kết quả đánh giá đầu vào."
                : $"Theo lộ trình hiện tại của bạn, tất cả các chủ đề đều ở mức trung bình — hãy học đều các phần.";
        }

        if (q.Contains("bắt đầu") || q.Contains("đầu tiên") || q.Contains("học gì"))
        {
            var first = phases.OrderBy(p => p.WeekNumber).First();
            return $"Bạn nên bắt đầu với \"{first.TopicName}\" (tuần {first.WeekNumber}), " +
                   $"học {first.SessionsPerWeek} buổi/tuần. {first.Note ?? ""}".Trim();
        }

        var topicList = string.Join(", ", topics.Take(5));
        return $"Lộ trình {subjectName} của bạn gồm {phases.Count} giai đoạn: {topicList}" +
               (topics.Count > 5 ? "..." : ".") +
               " Bạn có thể hỏi cụ thể về từng chủ đề hoặc thời gian học.";
    }

    private static string PriorityLabel(string priority) => priority switch
    {
        "high"   => "cao",
        "medium" => "trung bình",
        _        => "nâng cao"
    };
}
