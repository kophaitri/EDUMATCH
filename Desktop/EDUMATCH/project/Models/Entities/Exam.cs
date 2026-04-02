
namespace EduMatch.Models;

public class Exam
{
    public int Id { get; set; }
    public int? SessionId { get; set; }
    public string TutorId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public int MaxRetakes { get; set; } = 2;
    public bool IsOpen { get; set; } = false;
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public string? ExamFileUrl { get; set; }
    public ExamStatus Status { get; set; } = ExamStatus.Draft;
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Session? Session { get; set; }
    public ApplicationUser Tutor { get; set; } = null!;
    public ICollection<ExamQuestion> Questions { get; set; } = new List<ExamQuestion>();
    public ICollection<ExamSubmission> Submissions { get; set; } = new List<ExamSubmission>();
}

public class ExamQuestion
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? PassageText { get; set; }
    public int PartNumber { get; set; } = 0;
    public string QuestionType { get; set; } = "MultipleChoice";
    public int Points { get; set; }
    public int DisplayOrder { get; set; }

    public Exam Exam { get; set; } = null!;
    public ICollection<ExamAnswerOption> AnswerOptions { get; set; } = new List<ExamAnswerOption>();
}

public class ExamAnswerOption
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    public string OptionText { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int DisplayOrder { get; set; }

    public ExamQuestion Question { get; set; } = null!;
}

public class ExamSubmission
{
    public int Id { get; set; }
    public int ExamId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }
    public decimal TotalScore { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPassed { get; set; }
    public SubmissionStatus Status { get; set; } = SubmissionStatus.InProgress;
    public decimal FraudScore { get; set; }
    public bool IsFlagged { get; set; }
    public int RetakeNumber { get; set; } = 1;
    public string? TutorComment { get; set; }
    public DateTime? GradedAt { get; set; }

    public Exam Exam { get; set; } = null!;
    public ApplicationUser Student { get; set; } = null!;
    public ICollection<SubmissionAnswer> Answers { get; set; } = new List<SubmissionAnswer>();
    public ICollection<FraudWarning> FraudWarnings { get; set; } = new List<FraudWarning>();
    public ICollection<RetakeRequest> RetakeRequests { get; set; } = new List<RetakeRequest>();
    public ICollection<ExamBehaviorLog> BehaviorLogs { get; set; } = new List<ExamBehaviorLog>();
    public ExamSuspiciousScore? SuspiciousScore { get; set; }
}

public class SubmissionAnswer
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public int QuestionId { get; set; }
    public int? SelectedOptionId { get; set; }
    public string? TextAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int PointsEarned { get; set; }
    public ExamAnswerOption? SelectedOption { get; set; } 
    public ExamSubmission Submission { get; set; } = null!;
    public ExamQuestion Question { get; set; } = null!;
}

public class FraudWarning
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string WarningType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;

    public ExamSubmission Submission { get; set; } = null!;
}

public class RetakeRequest
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsApproved { get; set; }
    public string? ResponseNote { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public ExamSubmission Submission { get; set; } = null!;
}

public class ExamBehaviorLog
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public long TimestampMs { get; set; }
    public string? Meta { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ExamSubmission Submission { get; set; } = null!;
}

public class ExamSuspiciousScore
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public float TotalScore { get; set; }
    public float PasteScore { get; set; }
    public float TabScore { get; set; }
    public float TimeScore { get; set; }
    public float StyleScore { get; set; }
    public string Level { get; set; } = "Clean";
    public string ReasonsJson { get; set; } = "[]";
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    public ExamSubmission Submission { get; set; } = null!;
}

public class StudentWritingProfile
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public float AvgWordLength { get; set; }
    public float AvgSentenceLength { get; set; }
    public float VocabRichness { get; set; }
    public float PunctuationRatio { get; set; }
    public int SampleCount { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Student { get; set; } = null!;
}
