using Bogus;
using Microsoft.ML.Data;

namespace EduMatch.Services.ML;

public class ProgressTrainingData
{
    public float ScoreBefore      { get; set; }
    public float HoursPerWeek     { get; set; }
    public float AgeGroup         { get; set; }
    public float PreviousSessions { get; set; }
    [ColumnName("Label")]
    public float WeeksToImprove   { get; set; }
}

public class ProgressPrediction
{
    [ColumnName("Score")]
    public float WeeksToImprove { get; set; }
}

public class SyntheticDataGenerator
{
    public List<ProgressTrainingData> Generate()
    {
        var faker = new Faker("vi");
        var records = new List<ProgressTrainingData>();

        // 125 records: "yếu + lười"   score 10–45, hours 2–5
        for (int i = 0; i < 125; i++)
            records.Add(BuildRecord(faker, faker.Random.Int(10, 45), faker.Random.Float(2f, 5f)));

        // 125 records: "yếu + chăm"   score 10–45, hours 8–15
        for (int i = 0; i < 125; i++)
            records.Add(BuildRecord(faker, faker.Random.Int(10, 45), faker.Random.Float(8f, 15f)));

        // 150 records: "trung bình"   score 50–75, hours 4–10
        for (int i = 0; i < 150; i++)
            records.Add(BuildRecord(faker, faker.Random.Int(50, 75), faker.Random.Float(4f, 10f)));

        // 100 records: "khá giỏi"     score 76–95, hours 6–14
        for (int i = 0; i < 100; i++)
            records.Add(BuildRecord(faker, faker.Random.Int(76, 95), faker.Random.Float(6f, 14f)));

        // Shuffle before returning
        return records.OrderBy(_ => faker.Random.Int()).ToList();
    }

    private static ProgressTrainingData BuildRecord(Faker faker, int score, float hours)
    {
        float age  = faker.Random.Float(1f, 3f);   // 1=cấp1 2=cấp2 3=cấp3
        float prev = faker.Random.Float(0f, 30f);

        float baseWeeks  = (100f - score) / 12f;
        float hourFactor = 8f / Math.Max(hours, 1f);
        float ageFactor  = age >= 2.5f ? 0.85f : age >= 1.5f ? 1.0f : 1.15f;
        float expFactor  = 1f - MathF.Min(prev / 200f, 0.15f);
        float noise      = faker.Random.Float(0.85f, 1.15f);
        float weeks      = Math.Clamp(baseWeeks * hourFactor * ageFactor * expFactor * noise, 1f, 24f);

        return new ProgressTrainingData
        {
            ScoreBefore      = score,
            HoursPerWeek     = hours,
            AgeGroup         = age,
            PreviousSessions = prev,
            WeeksToImprove   = weeks
        };
    }
}
