using EduMatch.Controllers;

namespace EduMatch.Services;

public interface IExamBehaviorService
{
    Task<(bool Success, string? Error)> LogBehaviorAsync(string studentId, BehaviorLogRequest request);
}
