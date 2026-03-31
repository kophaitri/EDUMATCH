using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Http;

namespace EduMatch.Services;

/// <summary>
/// Parse file .docx theo định dạng tag chuẩn để tạo bài kiểm tra.
///
/// ── Thông tin đề ─────────────────────────────────────────────
///   [EXAM_TITLE] Đề thi TOEIC RC Test 01 - 2024
///   [EXAM_TIME]  75
///   [PASSING_SCORE] 450     (tùy chọn)
///   [MAX_RETAKES]   2       (tùy chọn)
///   [DESCRIPTION] Mô tả    (tùy chọn)
///
/// ── PART 5 – Câu đơn (không cần đoạn văn) ───────────────────
///   [PART:5]
///   [Q:101] Ms. Durkin asked for volunteers to help
///   [A] she
///   [B] her
///   [C] hers
///   [D] herself
///   [KEY:B]
///   _ with the employee fitness program.      ← phần tiếp theo sau đáp án
///
/// ── PART 6/7 – Câu đọc hiểu (có đoạn văn) ───────────────────
///   [PART:6]
///   [PASSAGE_START]
///   To: pmendoza@factmail.co
///   ...đoạn văn với (131) blank markers...
///   [PASSAGE_END]
///   [Q:131]
///   [A] Throughout the trial, you pay nothing and sign no contract.
///   [B] Weight-lifting classes are not currently available.
///   [C] A cash deposit is required when you sign up for membership.
///   [D] information
///   [KEY:D]
///
/// ── Ghi chú ──────────────────────────────────────────────────
///   • Parser chấp nhận cả ( thay vì [ và / thay vì ] (ngoặc lẫn lộn từ Word)
///   • Dòng _ hoặc _____ sau [KEY:x] được ghép vào cuối QuestionText
///   • Part 6/7: QuestionText có thể để trống (câu hỏi là blank trong passage)
/// </summary>
public class DocxExamParseResult
{
    public bool Success => Errors.Count == 0;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public int MaxRetakes { get; set; } = 2;
    public List<ParsedQuestion> Questions { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public class ParsedQuestion
{
    public int Number { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? PassageText { get; set; }
    public int PartNumber { get; set; }
    public int Points { get; set; } = 1;
    public bool ShuffleAnswers { get; set; } = true;
    public string CorrectKey { get; set; } = string.Empty;
    public List<ParsedOption> Options { get; set; } = new();
}

public class ParsedOption
{
    public string Label { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

public static class DocxExamParser
{
    // ── Bộ tags chuẩn (khớp chính xác với ExamImportAppService của src) ──
    private static readonly Regex TitleRegex        = new(@"\[EXAM_TITLE\]\s*(.+)", RegexOptions.IgnoreCase);
    private static readonly Regex TimeRegex         = new(@"\[EXAM_TIME\]\s*(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex PartRegex         = new(@"\[PART:(\d+)\]", RegexOptions.IgnoreCase);
    private static readonly Regex QuestionRegex     = new(@"\[Q:(\d+)\]\s*(.*)", RegexOptions.IgnoreCase);
    private static readonly Regex ShuffleRegex      = new(@"\[SHUFFLE:(TRUE|FALSE)\]", RegexOptions.IgnoreCase);
    private static readonly Regex AnswerRegex       = new(@"\[(A|B|C|D)\]\s*(.+)", RegexOptions.IgnoreCase);
    private static readonly Regex KeyRegex          = new(@"\[KEY:(A|B|C|D)\]", RegexOptions.IgnoreCase);
    private static readonly Regex PassageStartRegex = new(@"\[PASSAGE_START\]", RegexOptions.IgnoreCase);
    private static readonly Regex PassageEndRegex   = new(@"\[PASSAGE_END\]", RegexOptions.IgnoreCase);

    // ── Tags mở rộng cho EduMatch (không có trong src) ──
    private static readonly Regex PassingRegex  = new(@"\[PASSING_SCORE\]\s*(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex RetakesRegex  = new(@"\[MAX_RETAKES\]\s*(\d+)", RegexOptions.IgnoreCase);
    private static readonly Regex DescRegex     = new(@"\[DESCRIPTION\]\s*(.+)", RegexOptions.IgnoreCase);
    private static readonly Regex PointsRegex   = new(@"\[POINTS\]\s*(\d+)", RegexOptions.IgnoreCase);

    // ──────────────────────────────────────────────────────────
    public static DocxExamParseResult ParseFromFile(IFormFile file)
    {
        List<string> lines;
        try { lines = ReadDocxLines(file); }
        catch (Exception ex)
        {
            return new DocxExamParseResult { Errors = { $"Không thể đọc file: {ex.Message}" } };
        }
        return ParseLines(lines);
    }

    private static List<string> ReadDocxLines(IFormFile file)
    {
        using var stream = file.OpenReadStream();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart!.Document!.Body!;
        return body.Elements<Paragraph>()
            .Select(p => p.InnerText.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();
    }

    private static DocxExamParseResult ParseLines(List<string> lines)
    {
        var result = new DocxExamParseResult();
        ParsedQuestion? currentQ = null;
        bool inPassage = false;
        var passageLines = new List<string>();
        string? currentPassageText = null;
        int currentPartNumber = 0;

        // afterKey = true nghĩa là vừa xử lý [KEY:x], dòng tiếp theo không phải tag
        // thì là phần tiếp theo của câu hỏi (Part 5 dạng _ with the employee...)
        bool afterKey = false;

        foreach (var line in lines)
        {
            // ── Header tags ─────────────────────────────────
            var m = TitleRegex.Match(line);
            if (m.Success) { result.Title = m.Groups[1].Value.Trim(); afterKey = false; continue; }

            m = TimeRegex.Match(line);
            if (m.Success) { result.DurationMinutes = int.Parse(m.Groups[1].Value); afterKey = false; continue; }

            m = PassingRegex.Match(line);
            if (m.Success) { result.PassingScore = int.Parse(m.Groups[1].Value); afterKey = false; continue; }

            m = RetakesRegex.Match(line);
            if (m.Success) { result.MaxRetakes = int.Parse(m.Groups[1].Value); afterKey = false; continue; }

            m = DescRegex.Match(line);
            if (m.Success) { result.Description = m.Groups[1].Value.Trim(); afterKey = false; continue; }

            // ── [PART:n] ─────────────────────────────────────
            m = PartRegex.Match(line);
            if (m.Success)
            {
                SaveQuestion(ref currentQ, result);
                currentPartNumber = int.Parse(m.Groups[1].Value);
                // Reset passage khi sang Part mới (Part 5 không có passage)
                if (currentPartNumber == 5) currentPassageText = null;
                afterKey = false;
                continue;
            }

            // ── [PASSAGE_START] / [PASSAGE_END] ─────────────
            if (PassageStartRegex.IsMatch(line))
            {
                SaveQuestion(ref currentQ, result);
                inPassage = true;
                passageLines.Clear();
                afterKey = false;
                continue;
            }

            if (PassageEndRegex.IsMatch(line))
            {
                inPassage = false;
                currentPassageText = string.Join("\n", passageLines);
                passageLines.Clear();
                afterKey = false;
                continue;
            }

            if (inPassage) { passageLines.Add(line); continue; }

            // ── [Q:n] ────────────────────────────────────────
            m = QuestionRegex.Match(line);
            if (m.Success)
            {
                SaveQuestion(ref currentQ, result);
                afterKey = false;
                var qText = m.Groups[2].Value.Trim();
                var shuffleMatch = ShuffleRegex.Match(qText);
                var shuffle = true;
                if (shuffleMatch.Success)
                {
                    shuffle = shuffleMatch.Groups[1].Value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                    qText = ShuffleRegex.Replace(qText, "").Trim();
                }
                currentQ = new ParsedQuestion
                {
                    Number = int.Parse(m.Groups[1].Value),
                    QuestionText = qText,
                    PassageText = currentPassageText,
                    PartNumber = currentPartNumber,
                    ShuffleAnswers = shuffle,
                    Points = 1
                };
                continue;
            }

            if (currentQ == null) continue;

            // ── [SHUFFLE] trên dòng riêng ─────────────────────
            m = ShuffleRegex.Match(line);
            if (m.Success)
            {
                currentQ.ShuffleAnswers = m.Groups[1].Value.Equals("TRUE", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            // ── [POINTS] ─────────────────────────────────────
            m = PointsRegex.Match(line);
            if (m.Success) { currentQ.Points = int.Parse(m.Groups[1].Value); continue; }

            // ── [A]/[B]/[C]/[D] ──────────────────────────────
            m = AnswerRegex.Match(line);
            if (m.Success)
            {
                afterKey = false;
                currentQ.Options.Add(new ParsedOption
                {
                    Label = m.Groups[1].Value.ToUpper(),
                    Text = m.Groups[2].Value.Trim()
                });
                continue;
            }

            // ── [KEY:x] ──────────────────────────────────────
            m = KeyRegex.Match(line);
            if (m.Success)
            {
                currentQ.CorrectKey = m.Groups[1].Value.ToUpper();
                afterKey = true;
                continue;
            }

            // ── Continuation sau [KEY] (Part 5: phần còn lại của câu) ──
            if (afterKey)
            {
                // Thay _ hoặc _____ bằng chuỗi gạch dưới chuẩn
                var continuation = Regex.Replace(line, @"_+", "_____");
                currentQ.QuestionText = string.IsNullOrEmpty(currentQ.QuestionText)
                    ? continuation
                    : $"{currentQ.QuestionText} {continuation}";
                afterKey = false;
                continue;
            }

            // ── Nội dung câu hỏi trên dòng riêng (sau [Q:n] không có text) ──
            if (string.IsNullOrEmpty(currentQ.QuestionText))
                currentQ.QuestionText = line;
        }

        SaveQuestion(ref currentQ, result);

        if (inPassage) result.Errors.Add("Thiếu tag [PASSAGE_END]");

        Validate(result);
        return result;
    }

    private static void SaveQuestion(ref ParsedQuestion? q, DocxExamParseResult result)
    {
        if (q != null) { result.Questions.Add(q); q = null; }
    }

    private static void Validate(DocxExamParseResult result)
    {
        if (string.IsNullOrWhiteSpace(result.Title))
            result.Errors.Add("Thiếu tag [EXAM_TITLE]");

        if (result.DurationMinutes <= 0)
            result.Errors.Add("Thiếu hoặc sai tag [EXAM_TIME]");

        if (result.Questions.Count == 0)
            result.Errors.Add("Không tìm thấy câu hỏi nào (tag [Q:n])");

        foreach (var q in result.Questions)
        {
            // Part 6/7: QuestionText có thể trống nếu có passage (câu hỏi là blank trong passage)
            bool hasPasasge = !string.IsNullOrEmpty(q.PassageText);
            if (string.IsNullOrWhiteSpace(q.QuestionText) && !hasPasasge)
                result.Errors.Add($"Câu {q.Number}: Thiếu nội dung câu hỏi");

            var labels = q.Options.Select(o => o.Label).ToHashSet();
            foreach (var l in new[] { "A", "B", "C", "D" })
                if (!labels.Contains(l))
                    result.Errors.Add($"Câu {q.Number}: Thiếu đáp án [{l}]");

            if (string.IsNullOrEmpty(q.CorrectKey))
                result.Errors.Add($"Câu {q.Number}: Thiếu tag [KEY:...]");
            else if (!new[] { "A", "B", "C", "D" }.Contains(q.CorrectKey))
                result.Errors.Add($"Câu {q.Number}: [KEY] không hợp lệ (phải là A/B/C/D)");
        }
    }
}
