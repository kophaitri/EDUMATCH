using EduMatch.DTOs.Admin;
using Microsoft.AspNetCore.Identity;

namespace EduMatch.Services;

public class AdminComplaintService : IAdminComplaintService
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminComplaintService(EduMatchDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // ===================== REVIEW COMPLAINTS =====================

    public async Task<List<ReviewComplaintListDto>> GetReviewComplaintsAsync(string? status = null)
    {
        var query = _db.ReviewComplaints
            .Include(c => c.Review)
                .ThenInclude(r => r.Reviewee)
            .Include(c => c.Complainant)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(c => c.Status == status);

        return await query
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new ReviewComplaintListDto
            {
                Id = c.Id,
                ReviewId = c.ReviewId,
                ReviewRating = c.Review.Rating,
                ReviewComment = c.Review.Comment,
                RevieweeFullName = c.Review.Reviewee.FullName,
                ComplainantFullName = c.Complainant.FullName,
                ComplainantEmail = c.Complainant.Email ?? string.Empty,
                Reason = c.Reason,
                Status = c.Status,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<ReviewComplaintDetailDto?> GetReviewComplaintDetailAsync(int id)
    {
        var c = await _db.ReviewComplaints
            .Include(x => x.Review)
                .ThenInclude(r => r.Reviewer)
            .Include(x => x.Review)
                .ThenInclude(r => r.Reviewee)
            .Include(x => x.Complainant)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (c == null) return null;

        return new ReviewComplaintDetailDto
        {
            Id = c.Id,
            ReviewId = c.ReviewId,
            ReviewRating = c.Review.Rating,
            ReviewComment = c.Review.Comment,
            RevieweeFullName = c.Review.Reviewee.FullName,
            ReviewerId = c.Review.ReviewerId,
            ReviewerFullName = c.Review.Reviewer.FullName,
            ComplainantId = c.ComplainantId,
            ComplainantFullName = c.Complainant.FullName,
            ComplainantEmail = c.Complainant.Email ?? string.Empty,
            Reason = c.Reason,
            Status = c.Status,
            CreatedAt = c.CreatedAt
        };
    }

    public async Task<(bool Success, string Message)> HandleReviewComplaintAsync(int id, HandleReviewComplaintRequest request)
    {
        var complaint = await _db.ReviewComplaints
            .Include(c => c.Review)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (complaint == null) return (false, "Không tìm thấy khiếu nại.");

        complaint.Status = request.Status;

        // Xoá review nếu admin quyết định chấp nhận khiếu nại
        if (request.RemoveReview && complaint.Review != null)
        {
            _db.Reviews.Remove(complaint.Review);
        }

        await _db.SaveChangesAsync();
        return (true, $"Đã xử lý khiếu nại. Trạng thái: {request.Status}.");
    }

    // ===================== REPORTS =====================

    public async Task<List<ReportListDto>> GetReportsAsync(ReportStatus? status = null)
    {
        var query = _db.Reports
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(r => r.Status == status.Value);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReportListDto
            {
                Id = r.Id,
                ReporterId = r.ReporterId,
                ReporterFullName = r.Reporter.FullName,
                ReportedUserId = r.ReportedUserId,
                ReportedUserFullName = r.ReportedUser.FullName,
                Reason = r.Reason,
                Details = r.Details,
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt
            })
            .ToListAsync();
    }

    public async Task<ReportListDto?> GetReportDetailAsync(int id)
    {
        return await _db.Reports
            .Include(r => r.Reporter)
            .Include(r => r.ReportedUser)
            .Where(r => r.Id == id)
            .Select(r => new ReportListDto
            {
                Id = r.Id,
                ReporterId = r.ReporterId,
                ReporterFullName = r.Reporter.FullName,
                ReportedUserId = r.ReportedUserId,
                ReportedUserFullName = r.ReportedUser.FullName,
                Reason = r.Reason,
                Details = r.Details,
                Status = r.Status,
                AdminNote = r.AdminNote,
                CreatedAt = r.CreatedAt,
                ResolvedAt = r.ResolvedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<(bool Success, string Message)> HandleReportAsync(int id, HandleReportRequest request)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report == null) return (false, "Không tìm thấy báo cáo.");

        report.Status = request.Status;
        report.AdminNote = request.AdminNote;
        report.ResolvedAt = request.Status is ReportStatus.Resolved or ReportStatus.Rejected
            ? DateTime.UtcNow
            : null;

        // Khoá user bị báo cáo nếu admin yêu cầu
        if (request.LockReportedUser && request.Status == ReportStatus.Resolved)
        {
            var reportedUser = await _userManager.FindByIdAsync(report.ReportedUserId);
            if (reportedUser != null)
            {
                reportedUser.IsActive = false;
                reportedUser.LockoutEnabled = true;
                reportedUser.LockoutEnd = DateTimeOffset.MaxValue;
                reportedUser.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(reportedUser);
            }
        }

        await _db.SaveChangesAsync();
        return (true, "Đã xử lý báo cáo thành công.");
    }

    // ===================== FRAUD WARNINGS =====================

    public async Task<List<FraudWarningListDto>> GetFraudWarningsAsync(bool? flaggedOnly = null)
    {
        var query = _db.FraudWarnings
            .Include(f => f.Submission)
                .ThenInclude(s => s.Student)
            .Include(f => f.Submission)
                .ThenInclude(s => s.Exam)
            .AsQueryable();

        if (flaggedOnly == true)
            query = query.Where(f => f.Submission.IsFlagged);

        return await query
            .OrderByDescending(f => f.DetectedAt)
            .Select(f => new FraudWarningListDto
            {
                Id = f.Id,
                SubmissionId = f.SubmissionId,
                WarningType = f.WarningType,
                Details = f.Details,
                DetectedAt = f.DetectedAt,
                StudentId = f.Submission.StudentId,
                StudentFullName = f.Submission.Student.FullName,
                ExamTitle = f.Submission.Exam.Title,
                FraudScore = f.Submission.FraudScore,
                IsFlagged = f.Submission.IsFlagged,
                SubmissionStatus = f.Submission.Status,
                SubmittedAt = f.Submission.SubmittedAt
            })
            .ToListAsync();
    }
}
