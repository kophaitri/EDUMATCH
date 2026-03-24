using EduMatch.DTOs.Admin;
using EduMatch.Models.Enums;
using Microsoft.AspNetCore.Identity;

namespace EduMatch.Services;

public class AdminTicketService : IAdminTicketService
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminTicketService(EduMatchDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<List<TicketListDto>> GetTicketsAsync(
        TicketStatus? status = null,
        TicketPriority? priority = null,
        string? search = null)
    {
        var query = _db.SupportTickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .Include(t => t.Replies)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        if (priority.HasValue)
            query = query.Where(t => t.Priority == priority.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.Subject.Contains(search) ||
                t.User.FullName.Contains(search) ||
                t.User.Email!.Contains(search));

        return await query
            .OrderByDescending(t => t.Priority)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => new TicketListDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserFullName = t.User.FullName,
                UserEmail = t.User.Email ?? string.Empty,
                Subject = t.Subject,
                Status = t.Status,
                Priority = t.Priority,
                AssignedToId = t.AssignedToId,
                AssignedToFullName = t.AssignedTo != null ? t.AssignedTo.FullName : null,
                CreatedAt = t.CreatedAt,
                ResolvedAt = t.ResolvedAt,
                ReplyCount = t.Replies.Count
            })
            .ToListAsync();
    }

    public async Task<TicketDetailDto?> GetTicketDetailAsync(int id)
    {
        var ticket = await _db.SupportTickets
            .Include(t => t.User)
            .Include(t => t.AssignedTo)
            .Include(t => t.Replies)
                .ThenInclude(r => r.Sender)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return null;

        return new TicketDetailDto
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            UserFullName = ticket.User.FullName,
            UserEmail = ticket.User.Email ?? string.Empty,
            UserAvatarUrl = ticket.User.AvatarUrl,
            Subject = ticket.Subject,
            Description = ticket.Description,
            Status = ticket.Status,
            Priority = ticket.Priority,
            AssignedToId = ticket.AssignedToId,
            AssignedToFullName = ticket.AssignedTo?.FullName,
            CreatedAt = ticket.CreatedAt,
            ResolvedAt = ticket.ResolvedAt,
            Replies = ticket.Replies
                .OrderBy(r => r.CreatedAt)
                .Select(r => new TicketReplyDto
                {
                    Id = r.Id,
                    SenderId = r.SenderId,
                    SenderFullName = r.Sender.FullName,
                    SenderAvatarUrl = r.Sender.AvatarUrl,
                    Content = r.Content,
                    IsStaffReply = r.IsStaffReply,
                    CreatedAt = r.CreatedAt
                })
                .ToList()
        };
    }

    public async Task<(bool Success, string Message)> ReplyToTicketAsync(
        int id, string staffId, ReplyTicketRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
            return (false, "Nội dung trả lời không được để trống.");

        var ticket = await _db.SupportTickets.FindAsync(id);
        if (ticket == null) return (false, "Không tìm thấy ticket.");

        if (ticket.Status == TicketStatus.Closed)
            return (false, "Ticket đã đóng, không thể trả lời.");

        _db.TicketReplies.Add(new TicketReply
        {
            TicketId = id,
            SenderId = staffId,
            Content = request.Content.Trim(),
            IsStaffReply = true,
            CreatedAt = DateTime.UtcNow
        });

        // Cập nhật trạng thái nếu có
        if (request.UpdateStatus.HasValue)
        {
            ticket.Status = request.UpdateStatus.Value;
            if (request.UpdateStatus.Value == TicketStatus.Resolved)
                ticket.ResolvedAt = DateTime.UtcNow;
        }
        else if (ticket.Status == TicketStatus.Open)
        {
            // Tự động chuyển sang InProgress khi staff trả lời lần đầu
            ticket.Status = TicketStatus.InProgress;
        }

        await _db.SaveChangesAsync();
        return (true, "Đã trả lời ticket thành công.");
    }

    public async Task<(bool Success, string Message)> AssignTicketAsync(int id, AssignTicketRequest request)
    {
        var ticket = await _db.SupportTickets.FindAsync(id);
        if (ticket == null) return (false, "Không tìm thấy ticket.");

        // Validate người được phân công phải là Admin
        if (!string.IsNullOrWhiteSpace(request.AssignedToId))
        {
            var staff = await _userManager.FindByIdAsync(request.AssignedToId);
            if (staff == null) return (false, "Không tìm thấy nhân viên.");

            var roles = await _userManager.GetRolesAsync(staff);
            if (!roles.Contains("Admin"))
                return (false, "Chỉ có thể phân công cho tài khoản Admin.");
        }

        ticket.AssignedToId = string.IsNullOrWhiteSpace(request.AssignedToId)
            ? null
            : request.AssignedToId;

        await _db.SaveChangesAsync();

        return (true, string.IsNullOrWhiteSpace(request.AssignedToId)
            ? "Đã huỷ phân công ticket."
            : "Đã phân công ticket thành công.");
    }

    public async Task<(bool Success, string Message)> UpdateTicketStatusAsync(int id, UpdateTicketStatusRequest request)
    {
        var ticket = await _db.SupportTickets.FindAsync(id);
        if (ticket == null) return (false, "Không tìm thấy ticket.");

        ticket.Status = request.Status;

        if (request.Priority.HasValue)
            ticket.Priority = request.Priority.Value;

        if (request.Status == TicketStatus.Resolved && ticket.ResolvedAt == null)
            ticket.ResolvedAt = DateTime.UtcNow;
        else if (request.Status != TicketStatus.Resolved)
            ticket.ResolvedAt = null;

        await _db.SaveChangesAsync();
        return (true, "Đã cập nhật trạng thái ticket.");
    }

    public async Task<List<StaffMemberDto>> GetStaffMembersAsync()
    {
        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Admin");
        if (adminRole == null) return new List<StaffMemberDto>();

        var adminIds = await _db.UserRoles
            .Where(ur => ur.RoleId == adminRole.Id)
            .Select(ur => ur.UserId)
            .ToListAsync();

        var admins = await _db.Users
            .Where(u => adminIds.Contains(u.Id))
            .ToListAsync();

        // Đếm tickets đang mở theo staff — 1 query duy nhất
        var ticketCounts = await _db.SupportTickets
            .Where(t => adminIds.Contains(t.AssignedToId!) &&
                        t.Status != TicketStatus.Resolved &&
                        t.Status != TicketStatus.Closed)
            .GroupBy(t => t.AssignedToId!)
            .Select(g => new { AdminId = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = admins.Select(admin => new StaffMemberDto
        {
            Id = admin.Id,
            FullName = admin.FullName,
            Email = admin.Email,
            AssignedTickets = ticketCounts.FirstOrDefault(t => t.AdminId == admin.Id)?.Count ?? 0
        }).OrderBy(s => s.AssignedTickets).ToList();

        return result;
    }
}
