using EduMatch.Models.Enums;

namespace EduMatch.DTOs.Admin;

public class TicketListDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public string? AssignedToId { get; set; }
    public string? AssignedToFullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int ReplyCount { get; set; }
}

public class TicketDetailDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketStatus Status { get; set; }
    public TicketPriority Priority { get; set; }
    public string? AssignedToId { get; set; }
    public string? AssignedToFullName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public List<TicketReplyDto> Replies { get; set; } = new();
}

public class TicketReplyDto
{
    public int Id { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderFullName { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsStaffReply { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReplyTicketRequest
{
    public string Content { get; set; } = string.Empty;
    public TicketStatus? UpdateStatus { get; set; }
}

public class AssignTicketRequest
{
    public string? AssignedToId { get; set; }
}

public class UpdateTicketStatusRequest
{
    public TicketStatus Status { get; set; }
    public TicketPriority? Priority { get; set; }
}

public class StaffMemberDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public int AssignedTickets { get; set; }
}
