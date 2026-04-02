using EduMatch.Models.Enums;

namespace EduMatch.Models;

public class BookingRequest
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string TutorId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int GradeLevelId { get; set; }
    public string? Message { get; set; }
    public DateTime PreferredStartDate { get; set; }
    public int SessionsPerWeek { get; set; }
    public int DurationWeeks { get; set; }
    public decimal SessionDurationHours { get; set; } = 1.5m;
    public decimal HourlyRate { get; set; }
    public decimal TotalAmount { get; set; }
    public string? PaymentOrderId { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public ApplicationUser Student { get; set; } = null!;
    public ApplicationUser Tutor { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public GradeLevel GradeLevel { get; set; } = null!;
}

public class Contract
{
    public int Id { get; set; }
    public int BookingRequestId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string TutorId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int GradeLevelId { get; set; }
    public decimal HourlyRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalSessions { get; set; }
    public int CompletedSessions { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Active;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    public BookingRequest BookingRequest { get; set; } = null!;
    public ApplicationUser Student { get; set; } = null!;
    public ApplicationUser Tutor { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public GradeLevel GradeLevel { get; set; } = null!;
    public ICollection<Session> Sessions { get; set; } = new List<Session>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

public class Session
{
    public int Id { get; set; }
    public int ContractId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;
    public string? Notes { get; set; }
    public string? MeetingLink { get; set; }
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? TutorCompletedAt { get; set; }
    public DateTime? StudentConfirmedAt { get; set; }
    public bool EarningReleased { get; set; } = false;

    public Contract Contract { get; set; } = null!;
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}

public class WithdrawalRequest
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public ApplicationUser Tutor { get; set; } = null!;
}
