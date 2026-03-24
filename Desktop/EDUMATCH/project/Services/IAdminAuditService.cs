using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public interface IAdminAuditService
{
    Task<(List<AuditLogListDto> Items, int TotalCount)> GetAuditLogsAsync(
        string? action = null,
        string? entityType = null,
        string? userId = null,
        int page = 1,
        int pageSize = 50);

    Task<AuditLogDetailDto?> GetAuditLogDetailAsync(int id);
}
