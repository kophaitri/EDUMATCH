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
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
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
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Contract Contract { get; set; } = null!;
    public ICollection<Exam> Exams { get; set; } = new List<Exam>();
}
