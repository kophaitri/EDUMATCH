namespace EduMatch.Services;

public class SubmissionFraudSummary
{
    public int SubmissionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public float TotalScore { get; set; }
    public string Level { get; set; } = "Clean";
    public DateTime CalculatedAt { get; set; }
}

public class FraudDetailDto
{
    public int SubmissionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string ExamTitle { get; set; } = string.Empty;
    public float TotalScore { get; set; }
    public float PasteScore { get; set; }
    public float TabScore { get; set; }
    public float TimeScore { get; set; }
    public float StyleScore { get; set; }
    public string Level { get; set; } = "Clean";
    public List<string> Reasons { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public Dictionary<string, int> BehaviorEventCounts { get; set; } = new();
}

public interface IFraudService
{
    Task<List<SubmissionFraudSummary>> GetFraudSummariesForTutorAsync(string tutorId);
    Task<FraudDetailDto?> GetFraudDetailAsync(string tutorId, int submissionId);
}
