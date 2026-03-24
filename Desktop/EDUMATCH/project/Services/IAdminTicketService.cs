using EduMatch.DTOs.Admin;
using EduMatch.Models.Enums;

namespace EduMatch.Services;

public interface IAdminTicketService
{
    Task<List<TicketListDto>> GetTicketsAsync(TicketStatus? status = null, TicketPriority? priority = null, string? search = null);
    Task<TicketDetailDto?> GetTicketDetailAsync(int id);
    Task<(bool Success, string Message)> ReplyToTicketAsync(int id, string staffId, ReplyTicketRequest request);
    Task<(bool Success, string Message)> AssignTicketAsync(int id, AssignTicketRequest request);
    Task<(bool Success, string Message)> UpdateTicketStatusAsync(int id, UpdateTicketStatusRequest request);
    Task<List<StaffMemberDto>> GetStaffMembersAsync();
}
