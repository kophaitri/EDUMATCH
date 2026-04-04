using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;
using EduMatch.ViewModels;

namespace EduMatch.Controllers;

[Authorize(Policy = "StudentOnly")]
[Route("[controller]")]
public class RoadmapController : Controller
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IRoadmapService _roadmapService;
    private readonly IOllamaService _ollama;

    public RoadmapController(
        EduMatchDbContext db,
        UserManager<ApplicationUser> userManager,
        IRoadmapService roadmapService,
        IOllamaService ollama)
    {
        _db             = db;
        _userManager    = userManager;
        _roadmapService = roadmapService;
        _ollama         = ollama;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // ── GET /Roadmap ────────────────────────────────────────────────────────
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId   = GetUserId();
        var subjects = await _db.Subjects.Where(s => s.IsActive).ToListAsync();

        // Load all entry exams and student's submissions in one query each
        var entryExams = await _db.Exams
            .Where(e => e.IsEntryExam && e.Status == ExamStatus.Published && e.SubjectId != null)
            .Select(e => new { e.Id, e.SubjectId })
            .ToListAsync();

        var takenExamIds = await _db.ExamSubmissions
            .Where(s => s.StudentId == userId && s.Status == SubmissionStatus.Submitted)
            .Select(s => s.ExamId)
            .Distinct()
            .ToListAsync();

        var summaries = new List<SubjectRoadmapSummary>();
        foreach (var subject in subjects)
        {
            var roadmap    = await _roadmapService.GetActiveAsync(userId, subject.Id);
            var entryExam  = entryExams.FirstOrDefault(e => e.SubjectId == subject.Id);
            summaries.Add(new SubjectRoadmapSummary
            {
                SubjectId        = subject.Id,
                SubjectName      = subject.Name,
                HasActiveRoadmap = roadmap is not null,
                TotalPhases      = roadmap?.Phases.Count ?? 0,
                CompletedPhases  = roadmap?.Phases.Count(p => p.IsCompleted) ?? 0,
                GeneratedBy      = roadmap?.GeneratedBy,
                GeneratedAt      = roadmap?.GeneratedAt,
                EntryExamId      = entryExam?.Id,
                HasTakenEntryExam = entryExam is not null && takenExamIds.Contains(entryExam.Id)
            });
        }

        return View(new RoadmapIndexViewModel { Subjects = summaries });
    }

    // ── GET /Roadmap/Detail/{subjectId} ─────────────────────────────────────
    [HttpGet("Detail/{subjectId:int}")]
    public async Task<IActionResult> Detail(int subjectId)
    {
        var userId  = GetUserId();
        var roadmap = await _roadmapService.GetActiveAsync(userId, subjectId);

        if (roadmap is null)
        {
            TempData["ErrorMessage"] = "Chưa có lộ trình. Hãy làm bài test trước.";
            return RedirectToAction(nameof(Index));
        }

        var logs = await _roadmapService.GetProgressHistoryAsync(userId, subjectId);

        var vm = new RoadmapDetailViewModel
        {
            RoadmapId      = roadmap.Id,
            SubjectName    = roadmap.Subject?.Name ?? string.Empty,
            GeneratedBy    = roadmap.GeneratedBy,
            LlmExplanation = roadmap.LlmExplanation,
            GeneratedAt    = roadmap.GeneratedAt,
            EstimatedWeeks = roadmap.Phases.Any()
                ? (int)Math.Ceiling(roadmap.Phases.Sum(p => p.EstimatedWeeks))
                : 0,
            Phases = roadmap.Phases
                .OrderBy(p => p.WeekNumber)
                .Select(p => new PhaseViewModel
                {
                    WeekNumber      = p.WeekNumber,
                    TopicName       = p.TopicName,
                    Priority        = p.Priority,
                    SessionsPerWeek = p.SessionsPerWeek,
                    EstimatedWeeks  = p.EstimatedWeeks,
                    Note            = p.Note,
                    IsCompleted     = p.IsCompleted
                }).ToList(),
            Logs = logs.Select(l => new ProgressLogViewModel
            {
                TopicName   = l.TopicName,
                ScoreBefore = l.ScoreBefore,
                ScoreAfter  = l.ScoreAfter,
                LoggedAt    = l.LoggedAt
            }).ToList()
        };

        return View(vm);
    }

    // ── POST /Roadmap/Generate ──────────────────────────────────────────────
    [HttpPost("Generate")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int subjectId)
    {
        var userId = GetUserId();

        var hasAssessments = await _db.TopicAssessments
            .AnyAsync(a => a.StudentId == userId && a.SubjectId == subjectId);

        if (!hasAssessments)
        {
            TempData["ErrorMessage"] = "Bạn cần làm bài test đầu vào trước.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _userManager.GetUserAsync(User);
        var name = user?.FullName ?? user?.UserName ?? "Học viên";

        await _roadmapService.GenerateAsync(userId, subjectId, name);

        TempData["SuccessMessage"] = "Lộ trình học đã được tạo thành công!";
        return RedirectToAction(nameof(Detail), new { subjectId });
    }

    // ── POST /Roadmap/LogProgress ───────────────────────────────────────────
    [HttpPost("LogProgress")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LogProgress(LearningProgressLog log)
    {
        var userId      = GetUserId();
        log.StudentId   = userId;
        log.LoggedAt    = DateTime.UtcNow;

        await _roadmapService.LogProgressAsync(log);

        TempData["SuccessMessage"] = "Đã ghi nhận tiến độ!";
        return RedirectToAction(nameof(Detail), new { subjectId = log.SubjectId });
    }

    // ── POST /Roadmap/AskQuestion ───────────────────────────────────────────
    [HttpPost("AskQuestion")]
    public async Task<IActionResult> AskQuestion(int subjectId, string question)
    {
        var userId  = GetUserId();
        var roadmap = await _roadmapService.GetActiveAsync(userId, subjectId);
        var phases  = roadmap?.Phases.ToList() ?? new List<RoadmapPhase>();

        var subject = await _db.Subjects.FindAsync(subjectId);
        var subjectName = subject?.Name ?? string.Empty;

        var answer = await _ollama.AnswerQuestionAsync(question, subjectName, phases);
        return Json(new { answer });
    }
}
