using EduMatch.DTOs.Tutor;

namespace EduMatch.Services;

public interface ITutorDashboardService
{
    Task<TutorDashboardDto> GetDashboardAsync(string tutorUserId);
    Task<TutorSessionStatsDto> GetSessionStatsAsync(string tutorUserId, int? year = null);
    Task<TutorRevenueStatsDto> GetRevenueStatsAsync(string tutorUserId, int? year = null);
    Task<TutorReputationStatsDto> GetReputationStatsAsync(string tutorUserId);
}
