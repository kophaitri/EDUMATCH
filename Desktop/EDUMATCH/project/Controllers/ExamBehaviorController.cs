using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using EduMatch.Services;

namespace EduMatch.Controllers;

public record BehaviorEvent(string Type, long Timestamp);
public record BehaviorLogRequest(int SubmissionId, List<BehaviorEvent> Events);
public record TabSwitchRequest(int SubmissionId);
public record ScreenshotRequest(int SubmissionId, string Method);

[ApiController]
[Route("api/ExamBehavior")]
[Authorize(Policy = "StudentOnly")]
public class ExamBehaviorController : ControllerBase
{
    private readonly IExamBehaviorService _service;

    public ExamBehaviorController(IExamBehaviorService service)
    {
        _service = service;
    }

    [HttpPost("log")]
    public async Task<IActionResult> Log([FromBody] BehaviorLogRequest request)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var (success, error) = await _service.LogBehaviorAsync(studentId, request);
        if (!success)
            return Forbid();

        return Ok();
    }

    [HttpPost("tab-switch")]
    public async Task<IActionResult> TabSwitch([FromBody] TabSwitchRequest request)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var result = await _service.HandleTabSwitchAsync(studentId, request.SubmissionId);
        if (result == null)
            return Forbid();

        return Ok(result);
    }

    [HttpPost("screenshot")]
    public async Task<IActionResult> Screenshot([FromBody] ScreenshotRequest request)
    {
        var studentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(studentId))
            return Unauthorized();

        var ok = await _service.HandleScreenshotAsync(studentId, request.SubmissionId, request.Method);
        if (!ok) return Forbid();

        return Ok();
    }
}
