using EmailManagement.Application.Services;
using EmailManagement.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace EmailManagement.Worker;

public class EmailProcessingWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailProcessingWorker> _logger;
    private readonly WorkerOptions _options;

    public EmailProcessingWorker(
        IServiceProvider serviceProvider,
        ILogger<EmailProcessingWorker> logger,
        IOptions<WorkerOptions> options)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Email Processing Worker starting. Polling interval: {Interval}s, Batch size: {Batch}",
            _options.PollingIntervalSeconds, _options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = _serviceProvider.CreateAsyncScope();
                var processingService = scope.ServiceProvider.GetRequiredService<EmailProcessingService>();

                await processingService.ProcessBatchAsync(_options.BatchSize, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in email processing loop");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("Email Processing Worker stopped.");
    }
}