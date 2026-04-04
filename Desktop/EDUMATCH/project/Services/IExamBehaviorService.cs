using EduMatch.Controllers;

namespace EduMatch.Services;

public record TabSwitchResponse(int Count, string Action);

public interface IExamBehaviorService
{
    Task<(bool Success, string? Error)> LogBehaviorAsync(string studentId, BehaviorLogRequest request);
    Task<TabSwitchResponse?> HandleTabSwitchAsync(string studentId, int submissionId);
    Task<bool> HandleScreenshotAsync(string studentId, int submissionId, string method);
}
