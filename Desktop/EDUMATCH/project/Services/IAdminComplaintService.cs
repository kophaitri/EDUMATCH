using EduMatch.DTOs.Admin;

namespace EduMatch.Services;

public interface IAdminComplaintService
{
    // Review Complaints
    Task<List<ReviewComplaintListDto>> GetReviewComplaintsAsync(string? status = null);
    Task<ReviewComplaintDetailDto?> GetReviewComplaintDetailAsync(int id);
    Task<(bool Success, string Message)> HandleReviewComplaintAsync(int id, HandleReviewComplaintRequest request);

    // Reports
    Task<List<ReportListDto>> GetReportsAsync(ReportStatus? status = null);
    Task<ReportListDto?> GetReportDetailAsync(int id);
    Task<(bool Success, string Message)> HandleReportAsync(int id, HandleReportRequest request);

    // Fraud Warnings
    Task<List<FraudWarningListDto>> GetFraudWarningsAsync(bool? flaggedOnly = null);
}
