namespace EduMatch.DTOs.Admin;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalTutors { get; set; }
    public int TotalStudents { get; set; }
    public int TotalContracts { get; set; }
    public int ActiveContracts { get; set; }
    public int CompletedContracts { get; set; }
    public decimal TotalTransactionAmount { get; set; }
    public int PendingVerifications { get; set; }
    public List<RecentUserDto> RecentUsers { get; set; } = new();
    public List<RecentTransactionDto> RecentTransactions { get; set; } = new();
}

public class RecentUserDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

public class RecentTransactionDto
{
    public int Id { get; set; }
    public string UserFullName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }
    public TransactionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminStatsDto
{
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public int CancelledSessions { get; set; }
    public int TotalBookings { get; set; }
    public int PendingBookings { get; set; }
    public int AcceptedBookings { get; set; }
    public int RejectedBookings { get; set; }
    public List<MonthlyRevenueDto> MonthlyRevenue { get; set; } = new();
    public List<TopTutorDto> TopTutors { get; set; } = new();
}

public class MonthlyRevenueDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
}

public class TopTutorDto
{
    public string TutorId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TotalSessions { get; set; }
    public decimal AvgRating { get; set; }
}
