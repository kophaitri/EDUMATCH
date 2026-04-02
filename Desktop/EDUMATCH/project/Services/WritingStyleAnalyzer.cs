namespace EduMatch.Services;

public class StyleFeatures
{
    public float AvgWordLength { get; set; }
    public float AvgSentenceLength { get; set; }
    public float VocabRichness { get; set; }
    public float PunctuationRatio { get; set; }
}

public class WritingStyleAnalyzer
{
    private static readonly char[] SentenceDelimiters = ['.', '!', '?'];
    private static readonly char[] PunctuationChars = ['.', ',', '!', '?', ';', ':', '-', '(', ')', '"', '\''];

    public StyleFeatures ExtractFeatures(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new StyleFeatures();

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return new StyleFeatures();

        var sentences = text.Split(SentenceDelimiters, StringSplitOptions.RemoveEmptyEntries);
        var sentenceCount = Math.Max(sentences.Length, 1);

        var totalChars = text.Length;
        var punctCount = text.Count(c => PunctuationChars.Contains(c));
        var distinctWords = words.Select(w => w.ToLowerInvariant().Trim(',', '.', '!', '?', ';', ':')).Distinct().Count();

        return new StyleFeatures
        {
            AvgWordLength = (float)words.Average(w => w.Length),
            AvgSentenceLength = (float)words.Length / sentenceCount,
            VocabRichness = (float)distinctWords / words.Length,
            PunctuationRatio = totalChars > 0 ? (float)punctCount / totalChars : 0f
        };
    }

    public float Compare(string? newText, StudentWritingProfile profile)
    {
        if (profile.SampleCount < 2)
            return 0f;

        var features = ExtractFeatures(newText);
        if (features.AvgWordLength == 0)
            return 0f;

        float Divergence(float a, float b) => b == 0 ? 0 : Math.Abs(a - b) / Math.Max(b, 1f);

        var d1 = Divergence(features.AvgWordLength, profile.AvgWordLength);
        var d2 = Divergence(features.AvgSentenceLength, profile.AvgSentenceLength);
        var d3 = Divergence(features.VocabRichness, profile.VocabRichness);
        var d4 = Divergence(features.PunctuationRatio, profile.PunctuationRatio);

        var avg = (d1 + d2 + d3 + d4) / 4f;
        return Math.Clamp(avg, 0f, 1f);
    }

    public async Task UpdateProfileAsync(string studentId, string? text, EduMatchDbContext db)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var features = ExtractFeatures(text);
        if (features.AvgWordLength == 0)
            return;

        var profile = await db.StudentWritingProfiles
            .FirstOrDefaultAsync(p => p.StudentId == studentId);

        if (profile == null)
        {
            profile = new StudentWritingProfile
            {
                StudentId = studentId,
                AvgWordLength = features.AvgWordLength,
                AvgSentenceLength = features.AvgSentenceLength,
                VocabRichness = features.VocabRichness,
                PunctuationRatio = features.PunctuationRatio,
                SampleCount = 1,
                UpdatedAt = DateTime.UtcNow
            };
            db.StudentWritingProfiles.Add(profile);
        }
        else
        {
            var n = profile.SampleCount;
            profile.AvgWordLength = (profile.AvgWordLength * n + features.AvgWordLength) / (n + 1);
            profile.AvgSentenceLength = (profile.AvgSentenceLength * n + features.AvgSentenceLength) / (n + 1);
            profile.VocabRichness = (profile.VocabRichness * n + features.VocabRichness) / (n + 1);
            profile.PunctuationRatio = (profile.PunctuationRatio * n + features.PunctuationRatio) / (n + 1);
            profile.SampleCount = n + 1;
            profile.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }
}
