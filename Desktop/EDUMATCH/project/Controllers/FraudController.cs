using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;

namespace EduMatch.Controllers;

[Authorize(Policy = "TutorOnly")]
public class FraudController : Controller
{
    private readonly IFraudService _fraudService;

    public FraudController(IFraudService fraudService)
    {
        _fraudService = fraudService;
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    public async Task<IActionResult> Index()
    {
        var tutorId = GetUserId();
        if (string.IsNullOrEmpty(tutorId)) return Challenge();

        try
        {
            var summaries = await _fraudService.GetFraudSummariesForTutorAsync(tutorId);
            return View(summaries);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải dữ liệu giám sát: " + ex.Message;
            return View(new List<SubmissionFraudSummary>());
        }
    }

    public async Task<IActionResult> Detail(int submissionId)
    {
        var tutorId = GetUserId();
        if (string.IsNullOrEmpty(tutorId)) return Challenge();

        try
        {
            var detail = await _fraudService.GetFraudDetailAsync(tutorId, submissionId);
            if (detail == null) return NotFound();
            return View(detail);
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = "Không thể tải chi tiết: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}
