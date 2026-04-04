namespace EduMatch.ViewModels;

public class RoadmapIndexViewModel
{
    public List<SubjectRoadmapSummary> Subjects { get; set; } = new();
}

public class SubjectRoadmapSummary
{
    public int    SubjectId        { get; set; }
    public string SubjectName      { get; set; } = null!;
    public bool   HasActiveRoadmap { get; set; }
    public int    TotalPhases      { get; set; }
    public int    CompletedPhases  { get; set; }
    public int    ProgressPercent  =>
        TotalPhases == 0 ? 0 : (int)((float)CompletedPhases / TotalPhases * 100);
    public string?   GeneratedBy  { get; set; }
    public DateTime? GeneratedAt  { get; set; }

    // Entry exam
    public int?  EntryExamId       { get; set; }
    public bool  HasTakenEntryExam { get; set; }
}

public class RoadmapDetailViewModel
{
    public int    RoadmapId      { get; set; }
    public string SubjectName    { get; set; } = null!;
    public string GeneratedBy    { get; set; } = null!;
    public string? LlmExplanation { get; set; }
    public DateTime GeneratedAt  { get; set; }
    public int    EstimatedWeeks { get; set; }
    public List<PhaseViewModel>       Phases { get; set; } = new();
    public List<ProgressLogViewModel> Logs   { get; set; } = new();
}

public class PhaseViewModel
{
    public int    WeekNumber      { get; set; }
    public string TopicName       { get; set; } = null!;
    public string Priority        { get; set; } = null!;
    public int    SessionsPerWeek { get; set; }
    public float  EstimatedWeeks  { get; set; }
    public string? Note           { get; set; }
    public bool   IsCompleted     { get; set; }
}

public class ProgressLogViewModel
{
    public string   TopicName   { get; set; } = null!;
    public int      ScoreBefore { get; set; }
    public int      ScoreAfter  { get; set; }
    public int      Improvement => ScoreAfter - ScoreBefore;
    public DateTime LoggedAt    { get; set; }
}
