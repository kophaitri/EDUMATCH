namespace EduMatch.Models;

public class TicketReply
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsStaffReply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public SupportTicket Ticket { get; set; } = null!;
    public ApplicationUser Sender { get; set; } = null!;
}
