using EduMatch.DTOs.Tutor;

namespace EduMatch.Services;

public class TutorDashboardService : ITutorDashboardService
{
    private static readonly string[] MonthNames =
    [
        "", "Tháng 1", "Tháng 2", "Tháng 3", "Tháng 4",
        "Tháng 5", "Tháng 6", "Tháng 7", "Tháng 8",
        "Tháng 9", "Tháng 10", "Tháng 11", "Tháng 12"
    ];

    private readonly EduMatchDbContext _db;

    public TutorDashboardService(EduMatchDbContext db)
    {
        _db = db;
    }

    public async Task<TutorDashboardDto> GetDashboardAsync(string tutorUserId)
    {
        var now = DateTime.UtcNow;

        var user = await _db.Users
            .Include(u => u.TutorProfile)
            .FirstOrDefaultAsync(u => u.Id == tutorUserId);

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == tutorUserId);

        var activeContracts = await _db.Contracts
            .CountAsync(c => c.TutorId == tutorUserId && c.Status == ContractStatus.Active);

        var pendingBookings = await _db.BookingRequests
            .CountAsync(b => b.TutorId == tutorUserId && b.Status == BookingStatus.Pending);

        var sessionsThisMonth = await _db.Sessions
            .Include(s => s.Contract)
            .CountAsync(s => s.Contract.TutorId == tutorUserId
                          && s.Status == SessionStatus.Completed
                          && s.ScheduledAt.Year == now.Year
                          && s.ScheduledAt.Month == now.Month);

        var upcomingSessions = await _db.Sessions
            .Include(s => s.Contract)
            .CountAsync(s => s.Contract.TutorId == tutorUserId
                          && s.Status == SessionStatus.Scheduled
                          && s.ScheduledAt >= now);

        var recentReviews = await _db.Reviews
            .Include(r => r.Reviewer)
            .Where(r => r.RevieweeId == tutorUserId)
            .OrderByDescending(r => r.CreatedAt)
            .Take(5)
            .Select(r => new TutorRecentReviewDto
            {
                Id = r.Id,
                ReviewerName = r.IsAnonymous ? "Ẩn danh" : r.Reviewer.FullName,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new TutorDashboardDto
        {
            FullName = user?.FullName ?? string.Empty,
            AvatarUrl = user?.AvatarUrl,
            IsVerified = user?.TutorProfile?.IsVerified ?? false,
            ReputationScore = user?.TutorProfile?.ReputationScore ?? 0,
            AvgRating = user?.TutorProfile?.AvgRating ?? 0,
            TotalReviews = user?.TutorProfile?.TotalReviews ?? 0,
            ActiveContracts = activeContracts,
            PendingBookings = pendingBookings,
            SessionsThisMonth = sessionsThisMonth,
            UpcomingSessions = upcomingSessions,
            WalletBalance = wallet?.Balance ?? 0,
            TotalEarned = wallet?.TotalEarned ?? 0,
            RecentReviews = recentReviews
        };
    }

    public async Task<TutorSessionStatsDto> GetSessionStatsAsync(string tutorUserId, int? year = null)
    {
        int targetYear = year ?? DateTime.UtcNow.Year;

        var sessions = await _db.Sessions
            .Include(s => s.Contract)
            .Where(s => s.Contract.TutorId == tutorUserId
                     && s.ScheduledAt.Year == targetYear)
            .ToListAsync();

        var months = Enumerable.Range(1, 12).Select(m => new TutorSessionMonthDto
        {
            Month = m,
            MonthName = MonthNames[m],
            Completed = sessions.Count(s => s.ScheduledAt.Month == m && s.Status == SessionStatus.Completed),
            Cancelled = sessions.Count(s => s.ScheduledAt.Month == m && s.Status == SessionStatus.Cancelled),
            Scheduled = sessions.Count(s => s.ScheduledAt.Month == m && s.Status == SessionStatus.Scheduled)
        }).ToList();

        return new TutorSessionStatsDto
        {
            Year = targetYear,
            Months = months,
            TotalCompleted = sessions.Count(s => s.Status == SessionStatus.Completed),
            TotalCancelled = sessions.Count(s => s.Status == SessionStatus.Cancelled),
            TotalScheduled = sessions.Count(s => s.Status == SessionStatus.Scheduled)
        };
    }

    public async Task<TutorRevenueStatsDto> GetRevenueStatsAsync(string tutorUserId, int? year = null)
    {
        int targetYear = year ?? DateTime.UtcNow.Year;

        // Ưu tiên dùng TutorRevenueStat nếu đã có dữ liệu
        var revenueStats = await _db.TutorRevenueStats
            .Where(r => r.TutorId == tutorUserId && r.Year == targetYear)
            .ToListAsync();

        List<TutorRevenueMonthDto> months;

        if (revenueStats.Count > 0)
        {
            months = Enumerable.Range(1, 12).Select(m =>
            {
                var stat = revenueStats.FirstOrDefault(r => r.Month == m);
                return new TutorRevenueMonthDto
                {
                    Month = m,
                    MonthName = MonthNames[m],
                    Revenue = stat?.TotalRevenue ?? 0,
                    Sessions = stat?.TotalSessions ?? 0,
                    Students = stat?.TotalStudents ?? 0
                };
            }).ToList();
        }
        else
        {
            // Fallback: tính từ completed sessions trong năm
            var contracts = await _db.Contracts
                .Include(c => c.Sessions)
                .Where(c => c.TutorId == tutorUserId)
                .ToListAsync();

            months = Enumerable.Range(1, 12).Select(m =>
            {
                var monthSessions = contracts
                    .SelectMany(c => c.Sessions
                        .Where(s => s.Status == SessionStatus.Completed
                                 && s.ScheduledAt.Year == targetYear
                                 && s.ScheduledAt.Month == m)
                        .Select(s => new { Contract = c, Session = s }))
                    .ToList();

                var revenue = monthSessions.Sum(x =>
                    x.Contract.HourlyRate * (x.Session.DurationMinutes / 60m));

                var studentIds = monthSessions
                    .Select(x => x.Contract.StudentId)
                    .Distinct()
                    .Count();

                return new TutorRevenueMonthDto
                {
                    Month = m,
                    MonthName = MonthNames[m],
                    Revenue = Math.Round(revenue, 2),
                    Sessions = monthSessions.Count,
                    Students = studentIds
                };
            }).ToList();
        }

        return new TutorRevenueStatsDto
        {
            Year = targetYear,
            Months = months,
            TotalRevenue = months.Sum(m => m.Revenue),
            TotalSessions = months.Sum(m => m.Sessions),
            TotalStudents = months.Max(m => m.Students)
        };
    }

    public async Task<TutorReputationStatsDto> GetReputationStatsAsync(string tutorUserId)
    {
        var profile = await _db.TutorProfiles
            .FirstOrDefaultAsync(p => p.UserId == tutorUserId);

        var reviews = await _db.Reviews
            .Where(r => r.RevieweeId == tutorUserId)
            .ToListAsync();

        var logs = await _db.ReputationLogs
            .Where(l => l.UserId == tutorUserId)
            .OrderByDescending(l => l.CreatedAt)
            .Take(20)
            .Select(l => new TutorReputationLogDto
            {
                Action = l.Action,
                PointsChange = l.PointsChange,
                Reason = l.Reason,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new TutorReputationStatsDto
        {
            CurrentScore = profile?.ReputationScore ?? 0,
            AvgRating = profile?.AvgRating ?? 0,
            TotalReviews = reviews.Count,
            Rating5 = reviews.Count(r => r.Rating == 5),
            Rating4 = reviews.Count(r => r.Rating == 4),
            Rating3 = reviews.Count(r => r.Rating == 3),
            Rating2 = reviews.Count(r => r.Rating == 2),
            Rating1 = reviews.Count(r => r.Rating == 1),
            RecentLogs = logs
        };
    }
}
