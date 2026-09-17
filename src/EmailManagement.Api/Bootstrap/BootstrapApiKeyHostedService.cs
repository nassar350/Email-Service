using EmailManagement.Application.Services;

namespace EmailManagement.Api.Bootstrap;

public class BootstrapApiKeyHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BootstrapApiKeyHostedService> _logger;

    public BootstrapApiKeyHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<BootstrapApiKeyHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!string.Equals(_configuration["API_KEY_BOOTSTRAP"], "true", StringComparison.OrdinalIgnoreCase))
            return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var apiKeyService = scope.ServiceProvider.GetRequiredService<ApiKeyService>();
            var created = await apiKeyService.BootstrapIfEmptyAsync("bootstrap", cancellationToken);
            if (created is not null)
            {
                Console.WriteLine("==========================================================");
                Console.WriteLine("  A bootstrap API key was generated for this instance:");
                Console.WriteLine($"  {created.RawKey}");
                Console.WriteLine("  Store it securely. It is displayed only once.");
                Console.WriteLine("==========================================================");
                _logger.LogInformation("Bootstrap API key generated with prefix {Prefix}", created.KeyPrefix);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootstrap API key generation failed (a key may already exist).");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}