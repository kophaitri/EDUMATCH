using Microsoft.ML;
using Microsoft.ML.Trainers.FastTree;

namespace EduMatch.Services.ML;

public class MLModelService
{
    private readonly MLContext _ml = new(seed: 42);
    private readonly string _modelPath;
    private ITransformer? _model;
    private PredictionEngine<ProgressTrainingData, ProgressPrediction>? _engine;
    private readonly object _lock = new();

    public MLModelService(IWebHostEnvironment env)
    {
        _modelPath = Path.Combine(env.ContentRootPath, "MLModels", "progress.zip");
    }

    public void Train(List<ProgressTrainingData> data)
    {
        var dataView = _ml.Data.LoadFromEnumerable(data);

        var pipeline = _ml.Transforms.Concatenate("Features",
                nameof(ProgressTrainingData.ScoreBefore),
                nameof(ProgressTrainingData.HoursPerWeek),
                nameof(ProgressTrainingData.AgeGroup),
                nameof(ProgressTrainingData.PreviousSessions))
            .Append(_ml.Regression.Trainers.FastTree(
                labelColumnName: "Label",
                numberOfTrees: 100,
                numberOfLeaves: 20,
                minimumExampleCountPerLeaf: 5,
                learningRate: 0.1));

        var model = pipeline.Fit(dataView);

        Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
        _ml.Model.Save(model, dataView.Schema, _modelPath);

        lock (_lock)
        {
            _model  = null;
            _engine = null;
        }
    }

    public float PredictWeeks(float score, float hours, float age, float prev)
    {
        EnsureEngineLoaded();
        if (_engine is null)
            return FallbackEstimate(score, hours);

        var prediction = _engine.Predict(new ProgressTrainingData
        {
            ScoreBefore      = score,
            HoursPerWeek     = hours,
            AgeGroup         = age,
            PreviousSessions = prev
        });

        return Math.Clamp(prediction.WeeksToImprove, 1f, 24f);
    }

    public bool IsModelReady() => File.Exists(_modelPath);

    private static float FallbackEstimate(float score, float hours)
        => Math.Clamp((100f - score) / 12f * (8f / Math.Max(hours, 1f)), 1f, 24f);

    private void EnsureEngineLoaded()
    {
        if (_engine is not null) return;
        lock (_lock)
        {
            if (_engine is not null) return;
            if (!File.Exists(_modelPath)) return;
            _model  = _ml.Model.Load(_modelPath, out _);
            _engine = _ml.Model.CreatePredictionEngine<ProgressTrainingData, ProgressPrediction>(_model);
        }
    }
}
