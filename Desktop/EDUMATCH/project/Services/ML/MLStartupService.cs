namespace EduMatch.Services.ML;

public class MLStartupService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MLStartupService> _logger;

    public MLStartupService(IServiceScopeFactory scopeFactory, ILogger<MLStartupService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope    = _scopeFactory.CreateScope();
        var mlService      = scope.ServiceProvider.GetRequiredService<MLModelService>();
        var generator      = scope.ServiceProvider.GetRequiredService<SyntheticDataGenerator>();

        if (mlService.IsModelReady())
        {
            _logger.LogInformation("Model đã có sẵn, bỏ qua training.");
            return Task.CompletedTask;
        }

        _ = Task.Run(() =>
        {
            try
            {
                var data = generator.Generate();
                mlService.Train(data);
                _logger.LogInformation("Train xong, model sẵn sàng.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi training ML model.");
            }
        }, cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
