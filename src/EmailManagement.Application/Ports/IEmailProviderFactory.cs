using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IEmailProviderFactory
{
    IEmailProvider GetProvider(ProviderType providerType);
}

public interface IEmailProviderDiagnostics
{
    string ProviderType { get; }
    Task<ProviderTestResult> TestAsync(CancellationToken ct = default);
}

public class ProviderTestResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static ProviderTestResult Ok(string message = "Provider is reachable") =>
        new() { Success = true, Message = message };

    public static ProviderTestResult Failed(string message) =>
        new() { Success = false, Message = message };
}