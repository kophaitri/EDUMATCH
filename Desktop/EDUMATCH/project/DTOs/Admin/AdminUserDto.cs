namespace EduMatch.DTOs.Admin;

public class AdminUserListDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUserDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool LockoutEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Tutor info
    public TutorProfileDetailDto? TutorProfile { get; set; }

    // Student info
    public StudentProfileDetailDto? StudentProfile { get; set; }
}

public class TutorProfileDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? Education { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal AvgRating { get; set; }
    public int TotalReviews { get; set; }
    public decimal HourlyRateMin { get; set; }
    public decimal HourlyRateMax { get; set; }
    public bool IsVerified { get; set; }
    public decimal ReputationScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<TutorCertificateDto> Certificates { get; set; } = new();
}

public class TutorCertificateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? FileUrl { get; set; }
    public bool IsVerified { get; set; }
}

public class StudentProfileDetailDto
{
    public string Id { get; set; } = string.Empty;
    public string? CurrentGrade { get; set; }
    public string? SchoolName { get; set; }
    public string? LearningGoals { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PendingTutorDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public string? Education { get; set; }
    public int YearsOfExperience { get; set; }
    public decimal HourlyRateMin { get; set; }
    public decimal HourlyRateMax { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PendingCertificates { get; set; }
}

public class VerifyTutorRequest
{
    public bool Approve { get; set; }
    public List<int> ApprovedCertificateIds { get; set; } = new();
    public string? Note { get; set; }
}

public class AdminStudentListDto
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? CurrentGrade { get; set; }
    public string? SchoolName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
