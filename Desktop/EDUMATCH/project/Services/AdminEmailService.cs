using EduMatch.DTOs.Admin;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace EduMatch.Services;

public class AdminEmailService : IAdminEmailService
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminEmailService(EduMatchDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ===================== EMAIL TEMPLATES =====================

    public async Task<List<EmailTemplateListDto>> GetEmailTemplatesAsync()
    {
        return await _db.EmailTemplates
            .OrderBy(t => t.TemplateCode)
            .Select(t => new EmailTemplateListDto
            {
                Id = t.Id,
                TemplateCode = t.TemplateCode,
                Subject = t.Subject,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();
    }

    public async Task<EmailTemplateDetailDto?> GetEmailTemplateByIdAsync(int id)
    {
        var t = await _db.EmailTemplates.FindAsync(id);
        if (t == null) return null;

        return new EmailTemplateDetailDto
        {
            Id = t.Id,
            TemplateCode = t.TemplateCode,
            Subject = t.Subject,
            BodyHtml = t.BodyHtml,
            BodyText = t.BodyText,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };
    }

    public async Task<(bool Success, string Message)> CreateEmailTemplateAsync(CreateEmailTemplateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TemplateCode))
            return (false, "TemplateCode không được để trống.");

        bool codeExists = await _db.EmailTemplates
            .AnyAsync(t => t.TemplateCode == request.TemplateCode);
        if (codeExists)
            return (false, $"TemplateCode '{request.TemplateCode}' đã tồn tại.");

        _db.EmailTemplates.Add(new EmailTemplate
        {
            TemplateCode = request.TemplateCode.Trim(),
            Subject = request.Subject.Trim(),
            BodyHtml = request.BodyHtml,
            BodyText = request.BodyText,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return (true, "Tạo email template thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateEmailTemplateAsync(int id, UpdateEmailTemplateRequest request)
    {
        var template = await _db.EmailTemplates.FindAsync(id);
        if (template == null) return (false, "Không tìm thấy template.");

        template.Subject = request.Subject.Trim();
        template.BodyHtml = request.BodyHtml;
        template.BodyText = request.BodyText;
        template.IsActive = request.IsActive;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return (true, "Cập nhật email template thành công.");
    }

    public async Task<(bool Success, string Message)> DeleteEmailTemplateAsync(int id)
    {
        var template = await _db.EmailTemplates.FindAsync(id);
        if (template == null) return (false, "Không tìm thấy template.");

        _db.EmailTemplates.Remove(template);
        await _db.SaveChangesAsync();
        return (true, "Xóa email template thành công.");
    }

    public async Task<(bool Success, string Message)> ToggleEmailTemplateActiveAsync(int id)
    {
        var template = await _db.EmailTemplates.FindAsync(id);
        if (template == null) return (false, "Không tìm thấy template.");

        template.IsActive = !template.IsActive;
        template.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, template.IsActive ? "Đã kích hoạt template." : "Đã vô hiệu hóa template.");
    }

    // ===================== EMAIL QUEUE =====================

    public async Task<List<EmailQueueListDto>> GetEmailQueueAsync()
    {
        return await _db.EmailQueue
            .OrderBy(q => q.Priority)
            .ThenBy(q => q.ScheduledAt ?? q.CreatedAt)
            .Select(q => new EmailQueueListDto
            {
                Id = q.Id,
                ToEmail = q.ToEmail,
                Subject = q.Subject,
                Priority = q.Priority,
                RetryCount = q.RetryCount,
                ScheduledAt = q.ScheduledAt,
                CreatedAt = q.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<(bool Success, string Message)> DeleteEmailQueueItemAsync(int id)
    {
        var item = await _db.EmailQueue.FindAsync(id);
        if (item == null) return (false, "Không tìm thấy mục trong hàng đợi.");

        _db.EmailQueue.Remove(item);
        await _db.SaveChangesAsync();
        return (true, "Đã xóa mục khỏi hàng đợi.");
    }

    public async Task<(bool Success, string Message)> ClearEmailQueueAsync()
    {
        var items = await _db.EmailQueue.ToListAsync();
        if (items.Count == 0) return (false, "Hàng đợi email đã trống.");

        _db.EmailQueue.RemoveRange(items);
        await _db.SaveChangesAsync();
        return (true, $"Đã xóa {items.Count} mục khỏi hàng đợi.");
    }

    // ===================== EMAIL LOG =====================

    public async Task<List<EmailLogListDto>> GetEmailLogsAsync(bool? successOnly = null, string? search = null, int page = 1, int pageSize = 50)
    {
        var query = _db.EmailLogs.AsQueryable();

        if (successOnly.HasValue)
            query = query.Where(l => l.IsSuccess == successOnly.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l => l.ToEmail.Contains(search) || l.Subject.Contains(search));

        return await query
            .OrderByDescending(l => l.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new EmailLogListDto
            {
                Id = l.Id,
                ToEmail = l.ToEmail,
                Subject = l.Subject,
                IsSuccess = l.IsSuccess,
                ErrorMessage = l.ErrorMessage,
                SentAt = l.SentAt
            })
            .ToListAsync();
    }

    public async Task<int> GetEmailLogCountAsync()
    {
        return await _db.EmailLogs.CountAsync();
    }

    // ===================== BROADCAST NOTIFICATION =====================

    public async Task<(bool Success, string Message, int SentCount)> BroadcastNotificationAsync(BroadcastNotificationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || string.IsNullOrWhiteSpace(request.Message))
            return (false, "Tiêu đề và nội dung không được để trống.", 0);

        List<string> userIds;

        if (!string.IsNullOrWhiteSpace(request.TargetRole))
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync(request.TargetRole);
            userIds = usersInRole.Select(u => u.Id).ToList();
        }
        else
        {
            userIds = await _db.Users.Select(u => u.Id).ToListAsync();
        }

        if (userIds.Count == 0)
            return (false, "Không có người dùng nào phù hợp để gửi thông báo.", 0);

        var notifications = userIds.Select(uid => new Notification
        {
            UserId = uid,
            Type = NotificationType.System,
            Title = request.Title.Trim(),
            Message = request.Message.Trim(),
            ActionUrl = request.ActionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        }).ToList();

        await _db.Notifications.AddRangeAsync(notifications);
        await _db.SaveChangesAsync();

        return (true, $"Đã gửi thông báo đến {notifications.Count} người dùng.", notifications.Count);
    }
}
