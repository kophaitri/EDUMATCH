using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public interface IAdminEmailService
{
    // EmailTemplate
    Task<List<EmailTemplateListDto>> GetEmailTemplatesAsync();
    Task<EmailTemplateDetailDto?> GetEmailTemplateByIdAsync(int id);
    Task<(bool Success, string Message)> CreateEmailTemplateAsync(CreateEmailTemplateRequest request);
    Task<(bool Success, string Message)> UpdateEmailTemplateAsync(int id, UpdateEmailTemplateRequest request);
    Task<(bool Success, string Message)> DeleteEmailTemplateAsync(int id);
    Task<(bool Success, string Message)> ToggleEmailTemplateActiveAsync(int id);

    // EmailQueue
    Task<List<EmailQueueListDto>> GetEmailQueueAsync();
    Task<(bool Success, string Message)> DeleteEmailQueueItemAsync(int id);
    Task<(bool Success, string Message)> ClearEmailQueueAsync();

    // EmailLog
    Task<List<EmailLogListDto>> GetEmailLogsAsync(bool? successOnly = null, string? search = null, int page = 1, int pageSize = 50);
    Task<int> GetEmailLogCountAsync();

    // Broadcast Notification
    Task<(bool Success, string Message, int SentCount)> BroadcastNotificationAsync(BroadcastNotificationRequest request);
}
