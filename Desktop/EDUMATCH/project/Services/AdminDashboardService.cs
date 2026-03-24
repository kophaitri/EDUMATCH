using EduMatch.DTOs.Admin;
using Microsoft.AspNetCore.Identity;

namespace EduMatch.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly EduMatchDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminDashboardService(EduMatchDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync()
    {
        // Đếm users theo role
        var tutorRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Tutor");
        var studentRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Student");

        var totalUsers = await _db.Users.CountAsync();
        var totalTutors = tutorRole != null
            ? await _db.UserRoles.CountAsync(ur => ur.RoleId == tutorRole.Id)
            : 0;
        var totalStudents = studentRole != null
            ? await _db.UserRoles.CountAsync(ur => ur.RoleId == studentRole.Id)
            : 0;

        // Hợp đồng
        var totalContracts = await _db.Contracts.CountAsync();
        var activeContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Active);
        var completedContracts = await _db.Contracts.CountAsync(c => c.Status == ContractStatus.Completed);

        // Tổng tiền nạp + thu nhập gia sư đã hoàn thành
        var totalTransactionAmount = await _db.Transactions
            .Where(t => t.Status == TransactionStatus.Completed
                     && (t.Type == TransactionType.Deposit || t.Type == TransactionType.Earning))
            .SumAsync(t => (decimal?)t.Amount) ?? 0;

        // Gia sư chờ xác minh
        var pendingVerifications = await _db.TutorProfiles.CountAsync(t => !t.IsVerified);

        // 10 user mới nhất kèm role
        var recentUsers = await _db.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(10)
            .ToListAsync();

        var recentUserDtos = new List<RecentUserDto>();
        foreach (var u in recentUsers)
        {
            var roles = await _userManager.GetRolesAsync(u);
            recentUserDtos.Add(new RecentUserDto
            {
                Id = u.Id,
                FullName = u.FullName,
                Email = u.Email,
                AvatarUrl = u.AvatarUrl,
                Role = roles.FirstOrDefault() ?? "Unknown",
                CreatedAt = u.CreatedAt,
                IsActive = u.IsActive
            });
        }

        // 10 giao dịch gần nhất
        var recentTransactions = await _db.Transactions
            .Include(t => t.Wallet).ThenInclude(w => w.User)
            .OrderByDescending(t => t.CreatedAt)
            .Take(10)
            .Select(t => new RecentTransactionDto
            {
                Id = t.Id,
                UserFullName = t.Wallet.User.FullName,
                Amount = t.Amount,
                Type = t.Type,
                Status = t.Status,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return new AdminDashboardDto
        {
            TotalUsers = totalUsers,
            TotalTutors = totalTutors,
            TotalStudents = totalStudents,
            TotalContracts = totalContracts,
            ActiveContracts = activeContracts,
            CompletedContracts = completedContracts,
            TotalTransactionAmount = totalTransactionAmount,
            PendingVerifications = pendingVerifications,
            RecentUsers = recentUserDtos,
            RecentTransactions = recentTransactions
        };
    }

    public async Task<AdminStatsDto> GetStatsAsync(int? year = null)
    {
        var targetYear = year ?? DateTime.UtcNow.Year;

        // Doanh thu từ TutorRevenueStats
        var totalRevenue = await _db.TutorRevenueStats
            .Where(s => s.Year == targetYear)
            .SumAsync(s => (decimal?)s.TotalRevenue) ?? 0;

        // Session stats — lọc theo năm được chọn
        var totalSessions = await _db.Sessions
            .CountAsync(s => s.ScheduledAt.Year == targetYear);
        var completedSessions = await _db.Sessions
            .CountAsync(s => s.ScheduledAt.Year == targetYear && s.Status == SessionStatus.Completed);
        var cancelledSessions = await _db.Sessions
            .CountAsync(s => s.ScheduledAt.Year == targetYear && s.Status == SessionStatus.Cancelled);

        // Booking stats — lọc theo năm được chọn
        var totalBookings = await _db.BookingRequests
            .CountAsync(b => b.CreatedAt.Year == targetYear);
        var pendingBookings = await _db.BookingRequests
            .CountAsync(b => b.CreatedAt.Year == targetYear && b.Status == BookingStatus.Pending);
        var acceptedBookings = await _db.BookingRequests
            .CountAsync(b => b.CreatedAt.Year == targetYear && b.Status == BookingStatus.Accepted);
        var rejectedBookings = await _db.BookingRequests
            .CountAsync(b => b.CreatedAt.Year == targetYear && b.Status == BookingStatus.Rejected);

        // Doanh thu theo tháng — ưu tiên TutorRevenueStats, fallback sang Transactions
        var revenueStatRows = await _db.TutorRevenueStats
            .Where(s => s.Year == targetYear)
            .ToListAsync();

        List<MonthlyRevenueDto> monthlyRevenue;

        if (revenueStatRows.Count > 0)
        {
            monthlyRevenue = revenueStatRows
                .GroupBy(s => s.Month)
                .Select(g => new MonthlyRevenueDto
                {
                    Year = targetYear,
                    Month = g.Key,
                    TotalRevenue = g.Sum(s => s.TotalRevenue),
                    TotalSessions = g.Sum(s => s.TotalSessions)
                })
                .OrderBy(m => m.Month)
                .ToList();
        }
        else
        {
            // Fallback: tính từ Earning transactions theo tháng
            monthlyRevenue = await _db.Transactions
                .Where(t => t.CreatedAt.Year == targetYear
                         && t.Status == TransactionStatus.Completed
                         && t.Type == TransactionType.Earning)
                .GroupBy(t => t.CreatedAt.Month)
                .Select(g => new MonthlyRevenueDto
                {
                    Year = targetYear,
                    Month = g.Key,
                    TotalRevenue = g.Sum(t => t.Amount),
                    TotalSessions = 0
                })
                .OrderBy(m => m.Month)
                .ToListAsync();
        }

        // Top 5 gia sư — join trong 1 query duy nhất
        var topTutorRaw = await _db.TutorRevenueStats
            .Where(s => s.Year == targetYear)
            .GroupBy(s => s.TutorId)
            .Select(g => new
            {
                TutorId = g.Key,
                TotalRevenue = g.Sum(s => s.TotalRevenue),
                TotalSessions = g.Sum(s => s.TotalSessions)
            })
            .OrderByDescending(g => g.TotalRevenue)
            .Take(5)
            .ToListAsync();

        var tutorIds = topTutorRaw.Select(t => t.TutorId).ToList();
        var tutorProfiles = await _db.TutorProfiles
            .Include(p => p.User)
            .Where(p => tutorIds.Contains(p.UserId))
            .ToListAsync();

        var topTutorDtos = topTutorRaw.Select(t =>
        {
            var profile = tutorProfiles.FirstOrDefault(p => p.UserId == t.TutorId);
            return new TopTutorDto
            {
                TutorId = t.TutorId,
                FullName = profile?.User.FullName ?? "Unknown",
                TotalRevenue = t.TotalRevenue,
                TotalSessions = t.TotalSessions,
                AvgRating = profile?.AvgRating ?? 0
            };
        }).ToList();

        return new AdminStatsDto
        {
            TotalRevenue = totalRevenue,
            TotalSessions = totalSessions,
            CompletedSessions = completedSessions,
            CancelledSessions = cancelledSessions,
            TotalBookings = totalBookings,
            PendingBookings = pendingBookings,
            AcceptedBookings = acceptedBookings,
            RejectedBookings = rejectedBookings,
            MonthlyRevenue = monthlyRevenue,
            TopTutors = topTutorDtos
        };
    }
}
