namespace EduMatch.Services.RoadmapEngine;

public class RuleBasedEngine : IRoadmapEngine
{
    public LearningRoadmap Generate(
        List<TopicAssessment> assessments,
        StudentProfile profile,
        int subjectId)
    {
        float hoursPerWeek = Math.Max(profile.WeeklyAvailableHours, 1f);

        // Step 1 & 2 — Classify and sort ascending by Score within each group
        var high   = assessments.Where(a => a.Score < 60)           .OrderBy(a => a.Score).ToList();
        var medium = assessments.Where(a => a.Score >= 60 && a.Score < 80).OrderBy(a => a.Score).ToList();
        var low    = assessments.Where(a => a.Score >= 80)           .OrderBy(a => a.Score).ToList();

        // Step 3 — Assign WeekNumber sequentially: high → medium → low
        var phases = new List<RoadmapPhase>();
        int week = 1;

        foreach (var a in high)
            phases.Add(BuildPhase(a, "high", week++, hoursPerWeek, profile));
        foreach (var a in medium)
            phases.Add(BuildPhase(a, "medium", week++, hoursPerWeek, profile));
        foreach (var a in low)
            phases.Add(BuildPhase(a, "low", week++, hoursPerWeek, profile));

        // Step 6 — Return roadmap
        return new LearningRoadmap
        {
            SubjectId   = subjectId,
            GeneratedBy = "RuleBased",
            Phases      = phases
        };
    }

    private static RoadmapPhase BuildPhase(
        TopicAssessment a,
        string priority,
        int weekNumber,
        float hoursPerWeek,
        StudentProfile profile)
    {
        // Step 1 — SessionsPerWeek
        int sessionsPerWeek = priority switch
        {
            "high"   => hoursPerWeek >= 10 ? 3 : 2,
            "medium" => 2,
            _        => 1   // low
        };

        // Step 4 — Note (Vietnamese)
        string note = priority switch
        {
            "high"   => $"Điểm {a.Score}/100 — cần củng cố gấp",
            "medium" => $"Điểm {a.Score}/100 — ôn tập thêm",
            _        => $"Điểm {a.Score}/100 — nâng cao"
        };

        // Step 5 — EstimatedWeeks clamped [1, 24]
        float raw = (100f - a.Score) / 12f * (8f / hoursPerWeek);
        float estimatedWeeks = Math.Clamp(raw, 1f, 24f);

        return new RoadmapPhase
        {
            WeekNumber      = weekNumber,
            TopicName       = a.TopicName,
            Priority        = priority,
            SessionsPerWeek = sessionsPerWeek,
            EstimatedWeeks  = estimatedWeeks,
            Note            = note
        };
    }
}
