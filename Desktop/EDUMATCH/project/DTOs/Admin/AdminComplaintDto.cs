namespace EduMatch.DTOs.Admin;

// ===== REVIEW COMPLAINTS =====
public class ReviewComplaintListDto
{
    public int Id { get; set; }
    public int ReviewId { get; set; }
    public int ReviewRating { get; set; }
    public string? ReviewComment { get; set; }
    public string RevieweeFullName { get; set; } = string.Empty;
    public string ComplainantFullName { get; set; } = string.Empty;
    public string ComplainantEmail { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class ReviewComplaintDetailDto : ReviewComplaintListDto
{
    public string ReviewerId { get; set; } = string.Empty;
    public string ReviewerFullName { get; set; } = string.Empty;
    public string ComplainantId { get; set; } = string.Empty;
}

public class HandleReviewComplaintRequest
{
    // "Approved" = xoá review | "Rejected" = giữ review
    public string Status { get; set; } = string.Empty;
    public bool RemoveReview { get; set; }
    public string? AdminNote { get; set; }
}

// ===== REPORTS =====
public class ReportListDto
{
    public int Id { get; set; }
    public string ReporterId { get; set; } = string.Empty;
    public string ReporterFullName { get; set; } = string.Empty;
    public string ReportedUserId { get; set; } = string.Empty;
    public string ReportedUserFullName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }
    public ReportStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class HandleReportRequest
{
    public ReportStatus Status { get; set; }
    public string? AdminNote { get; set; }
    public bool LockReportedUser { get; set; }
}

// ===== FRAUD WARNINGS =====
public class FraudWarningListDto
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string WarningType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }

    // Submission info
    public string StudentId { get; set; } = string.Empty;
    public string StudentFullName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public decimal FraudScore { get; set; }
    public bool IsFlagged { get; set; }
    public SubmissionStatus SubmissionStatus { get; set; }
    public DateTime? SubmittedAt { get; set; }
}
