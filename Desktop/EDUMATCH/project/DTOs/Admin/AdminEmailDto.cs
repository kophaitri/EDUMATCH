namespace EduMatch.DTOs.Admin;

// ===================== EMAIL TEMPLATES =====================

public class EmailTemplateListDto
{
    public int Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class EmailTemplateDetailDto
{
    public int Id { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateEmailTemplateRequest
{
    public string TemplateCode { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateEmailTemplateRequest
{
    public string Subject { get; set; } = string.Empty;
    public string BodyHtml { get; set; } = string.Empty;
    public string? BodyText { get; set; }
    public bool IsActive { get; set; }
}

// ===================== EMAIL QUEUE =====================

public class EmailQueueListDto
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public int Priority { get; set; }
    public int RetryCount { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===================== EMAIL LOG =====================

public class EmailLogListDto
{
    public int Id { get; set; }
    public string ToEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; }
}

// ===================== BROADCAST NOTIFICATION =====================

public class BroadcastNotificationRequest
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    /// <summary>null = gửi cho tất cả user, "Tutor", "Student", "Admin" = gửi theo role</summary>
    public string? TargetRole { get; set; }
}
