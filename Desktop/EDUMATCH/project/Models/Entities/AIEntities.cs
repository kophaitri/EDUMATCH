namespace EduMatch.Models;

public class TopicAssessment
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int Score { get; set; }
    public DateTime TakenAt { get; set; } = DateTime.UtcNow;

    public ExamSubmission Submission { get; set; } = null!;
    public ApplicationUser Student { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}

public class LearningRoadmap
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public string GeneratedBy { get; set; } = "RuleBased";
    public string? LlmExplanation { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAdjustedAt { get; set; }

    public ApplicationUser Student { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public ICollection<RoadmapPhase> Phases { get; set; } = new List<RoadmapPhase>();
}

public class RoadmapPhase
{
    public int Id { get; set; }
    public int RoadmapId { get; set; }
    public int WeekNumber { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public int SessionsPerWeek { get; set; }
    public float EstimatedWeeks { get; set; }
    public string? Note { get; set; }
    public bool IsCompleted { get; set; } = false;

    public LearningRoadmap Roadmap { get; set; } = null!;
}

public class LearningProgressLog
{
    public int Id { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public int SubjectId { get; set; }
    public int? SessionId { get; set; }
    public string TopicName { get; set; } = string.Empty;
    public int ScoreBefore { get; set; }
    public int ScoreAfter { get; set; }
    public float HoursStudied { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser Student { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
    public Session? Session { get; set; }
}
