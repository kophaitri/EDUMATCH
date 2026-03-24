namespace EduMatch.DTOs.Tutor;

public class TutorDashboardDto
{
    // Profile summary
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsVerified { get; set; }
    public decimal ReputationScore { get; set; }
    public decimal AvgRating { get; set; }
    public int TotalReviews { get; set; }

    // Counts
    public int ActiveContracts { get; set; }
    public int PendingBookings { get; set; }
    public int SessionsThisMonth { get; set; }
    public int UpcomingSessions { get; set; }

    // Wallet
    public decimal WalletBalance { get; set; }
    public decimal TotalEarned { get; set; }

    // Recent reviews
    public List<TutorRecentReviewDto> RecentReviews { get; set; } = new();
}

public class TutorRecentReviewDto
{
    public int Id { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ===================== SESSIONS STATS =====================

public class TutorSessionStatsDto
{
    public int Year { get; set; }
    public List<TutorSessionMonthDto> Months { get; set; } = new();
    public int TotalCompleted { get; set; }
    public int TotalCancelled { get; set; }
    public int TotalScheduled { get; set; }
}

public class TutorSessionMonthDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int Scheduled { get; set; }
    public int Total => Completed + Cancelled + Scheduled;
}

// ===================== REVENUE STATS =====================

public class TutorRevenueStatsDto
{
    public int Year { get; set; }
    public List<TutorRevenueMonthDto> Months { get; set; } = new();
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
    public int TotalStudents { get; set; }
}

public class TutorRevenueMonthDto
{
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
    public int Sessions { get; set; }
    public int Students { get; set; }
}

// ===================== REPUTATION STATS =====================

public class TutorReputationStatsDto
{
    public decimal CurrentScore { get; set; }
    public decimal AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public int Rating5 { get; set; }
    public int Rating4 { get; set; }
    public int Rating3 { get; set; }
    public int Rating2 { get; set; }
    public int Rating1 { get; set; }
    public List<TutorReputationLogDto> RecentLogs { get; set; } = new();
}

public class TutorReputationLogDto
{
    public string Action { get; set; } = string.Empty;
    public decimal PointsChange { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
}
