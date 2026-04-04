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
            $"- Tuần {p.WeekNumber}: {p.TopicName} | ưu tiên {p.Priority} | " +
            $"dự kiến {p.EstimatedWeeks:F1} tuần | {p.SessionsPerWeek} buổi/tuần" +
            (string.IsNullOrEmpty(p.Note) ? "" : $" | {p.Note}")));

        var prompt =
            $"Bạn là trợ lý học tập thân thiện.\n" +
            $"Học viên {studentName} vừa nhận lộ trình học môn {subjectName}:\n" +
            $"{phaseList}\n" +
            $"Giải thích ngắn gọn 3-4 câu tại sao lộ trình được sắp xếp như vậy.\n" +
            $"Dùng ngôn ngữ thân thiện, động viên. Tiếng Việt, không bullet point.";

        var (result, _) = await AskOllamaAsync(prompt, context: null);
        return !string.IsNullOrEmpty(result)
            ? result
            : FallbackExplain(phases, subjectName, studentName);
    }

    public async Task<(string Answer, string? ContextToken)> AnswerQuestionAsync(
        string question, string subjectName, List<RoadmapPhase> phases,
        string? contextToken = null)
    {
        string prompt;

        if (contextToken is null)
        {
            // Lần đầu: nhúng toàn bộ thông tin roadmap từ Rule engine + ML
            var sb = new StringBuilder();
            sb.AppendLine($"Bạn là trợ lý học tập môn {subjectName}. Hãy nhớ thông tin này xuyên suốt cuộc trò chuyện.");
            sb.AppendLine($"Lộ trình học được tạo bởi Rule-Based Engine + ML Model:");
            foreach (var p in phases)
            {
                sb.AppendLine(
                    $"- Tuần {p.WeekNumber}: {p.TopicName} | " +
                    $"Ưu tiên: {p.Priority} | " +
                    $"Dự kiến: {p.EstimatedWeeks:F1} tuần | " +
                    $"{p.SessionsPerWeek} buổi/tuần" +
                    (string.IsNullOrEmpty(p.Note) ? "" : $" | {p.Note}"));
            }
            sb.AppendLine($"Câu hỏi: {question}");
            sb.AppendLine("Trả lời ngắn gọn, chính xác bằng tiếng Việt.");
            prompt = sb.ToString();
        }
        else
        {
            // Lần sau: Ollama đã nhớ context, chỉ gửi câu hỏi mới
            prompt = question;
        }

        int[]? ctx = null;
        if (contextToken is not null)
        {
            try { ctx = JsonSerializer.Deserialize<int[]>(contextToken); }
            catch { ctx = null; }
        }

        var (answer, newCtx) = await AskOllamaAsync(prompt, context: ctx);

        if (!string.IsNullOrEmpty(answer))
        {
            string? newToken = newCtx is { Length: > 0 }
                ? JsonSerializer.Serialize(newCtx)
                : contextToken;
            return (answer, newToken);
        }

        return (FallbackAnswer(question, subjectName, phases), null);
    }

    // ── Ollama call ────────────────────────────────────────────────────────────
    private async Task<(string Response, int[]? Context)> AskOllamaAsync(
        string prompt, int[]? context)
    {
        try
        {
            object requestBody = context is { Length: > 0 }
                ? new { model = "llama3.2", prompt, stream = false, context }
                : new { model = "llama3.2", prompt, stream = false };

            var body = JsonSerializer.Serialize(requestBody);

            var response = await _http.PostAsync(
                "/api/generate",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Ollama trả về {Status}", response.StatusCode);
                return (string.Empty, null);
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var answer = root.TryGetProperty("response", out var val)
                ? val.GetString() ?? string.Empty
                : string.Empty;

            int[]? newContext = null;
            if (root.TryGetProperty("context", out var ctxProp) &&
                ctxProp.ValueKind == JsonValueKind.Array)
            {
                newContext = ctxProp.EnumerateArray()
                    .Select(e => e.GetInt32())
                    .ToArray();
            }

            return (answer, newContext);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex, "Ollama không khả dụng — dùng fallback");
            return (string.Empty, null);
        }
    }

    // ── Rule-based fallbacks (khi Ollama không chạy) ──────────────────────────
    private static string FallbackExplain(
        List<RoadmapPhase> phases, string subjectName, string studentName)
    {
        var highCount  = phases.Count(p => p.Priority == "high");
        var totalWeeks = (int)Math.Ceiling(phases.Sum(p => p.EstimatedWeeks));
        var firstTopic = phases.OrderBy(p => p.WeekNumber).FirstOrDefault()?.TopicName ?? "kiến thức nền";

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
        var q      = question.ToLower();
        var topics = phases.Select(p => p.TopicName).ToList();

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
