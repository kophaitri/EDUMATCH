using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync();
    Task<AdminStatsDto> GetStatsAsync(int? year = null);
}
