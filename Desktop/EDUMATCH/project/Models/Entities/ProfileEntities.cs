using EduMatch.Models.Enums;

namespace EduMatch.Models;

public class RefreshToken
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public bool IsRevoked { get; set; }

    public ApplicationUser User { get; set; } = null!;
}

public class TutorProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Education { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public decimal HourlyRateMin { get; set; }
    public decimal HourlyRateMax { get; set; }
    public string? VideoIntroUrl { get; set; }
    public bool IsVerified { get; set; }
    public decimal ReputationScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
    public ICollection<TutorSubject> TutorSubjects { get; set; } = new List<TutorSubject>();
    public ICollection<TutorCertificate> Certificates { get; set; } = new List<TutorCertificate>();
    public ICollection<TutorAvailability> Availabilities { get; set; } = new List<TutorAvailability>();
    public ICollection<TutorMediaFile> MediaFiles { get; set; } = new List<TutorMediaFile>();
}

public class StudentProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string UserId { get; set; } = string.Empty;
    public string? CurrentGrade { get; set; }
    public string? SchoolName { get; set; }
    public string? LearningGoals { get; set; }
    public int WeeklyAvailableHours { get; set; } = 6;
    public int? GradeLevelId { get; set; }
    public int? TotalSessionsCompleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}

public class Subject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<TutorSubject> TutorSubjects { get; set; } = new List<TutorSubject>();
}

public class GradeLevel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public ICollection<TutorSubject> TutorSubjects { get; set; } = new List<TutorSubject>();
}

public class TeachingStyle
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<TutorTeachingStyle> TutorTeachingStyles { get; set; } = new List<TutorTeachingStyle>();
}

public class TutorSubject
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int GradeLevelId { get; set; }
    public decimal HourlyRate { get; set; }

    public TutorProfile Tutor { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public GradeLevel GradeLevel { get; set; } = null!;
}

public class TutorTeachingStyle
{
    public string TutorId { get; set; } = string.Empty;
    public int TeachingStyleId { get; set; }

    public TutorProfile Tutor { get; set; } = null!;
    public TeachingStyle TeachingStyle { get; set; } = null!;
}

public class TutorCertificate
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? FileUrl { get; set; }
    public bool IsVerified { get; set; }

    public TutorProfile Tutor { get; set; } = null!;
}

public class TutorAvailability
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public TutorProfile Tutor { get; set; } = null!;
}

public class TutorMediaFile
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public TutorProfile Tutor { get; set; } = null!;
}

public class TutorPost
{
    public int Id { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Tutor { get; set; } = null!;
    public List<TutorPostImage> Images { get; set; } = new();
}

public class TutorPostImage
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public TutorPost Post { get; set; } = null!;
}
