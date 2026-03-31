using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EduMatch.Controllers;

/// <summary>
/// Cho phép học viên và gia sư tạo / xem / trả lời support ticket.
/// Admin xử lý ticket qua AdminController.
/// </summary>
[Authorize(Policy = "TutorOrStudent")]
public class SupportController : Controller
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public SupportController(EduMatchDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET: /Support/MyTickets
    public async Task<IActionResult> MyTickets()
    {
        var userId = GetUserId();
        var tickets = await _db.SupportTickets
            .Include(t => t.Replies)
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();

        return View(tickets);
    }

    // GET: /Support/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: /Support/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string subject, string description, string priority)
    {
        if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(description))
        {
            TempData["ErrorMessage"] = "Vui lòng điền đầy đủ tiêu đề và nội dung.";
            return View();
        }

        var ticketPriority = Enum.TryParse<TicketPriority>(priority, out var p) ? p : TicketPriority.Medium;

        _db.SupportTickets.Add(new SupportTicket
        {
            UserId = GetUserId(),
            Subject = subject.Trim(),
            Description = description.Trim(),
            Priority = ticketPriority,
            Status = TicketStatus.Open,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Gửi ticket thành công! Admin sẽ phản hồi sớm nhất có thể.";
        return RedirectToAction("MyTickets");
    }

    // GET: /Support/TicketDetail/{id}
    public async Task<IActionResult> TicketDetail(int id)
    {
        var userId = GetUserId();
        var ticket = await _db.SupportTickets
            .Include(t => t.Replies)
                .ThenInclude(r => r.Sender)
            .Include(t => t.AssignedTo)
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (ticket == null) return NotFound();
        return View(ticket);
    }

    // POST: /Support/Reply/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int id, string content)
    {
        var userId = GetUserId();
        var ticket = await _db.SupportTickets
            .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

        if (ticket == null) return NotFound();

        if (ticket.Status == TicketStatus.Closed)
        {
            TempData["ErrorMessage"] = "Ticket đã đóng, không thể trả lời thêm.";
            return RedirectToAction("TicketDetail", new { id });
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            TempData["ErrorMessage"] = "Nội dung phản hồi không được để trống.";
            return RedirectToAction("TicketDetail", new { id });
        }

        _db.TicketReplies.Add(new TicketReply
        {
            TicketId = id,
            SenderId = userId,
            Content = content.Trim(),
            IsStaffReply = false,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã gửi phản hồi.";
        return RedirectToAction("TicketDetail", new { id });
    }
}
