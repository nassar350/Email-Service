namespace EmailManagement.Application.DTOs.Requests;

public record CreateProviderRequest
{
    public string ProviderType { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string? Configuration { get; init; }
    public bool IsDefault { get; init; }
}

public record UpdateProviderRequest
{
    public string? ProviderName { get; init; }
    public string? Configuration { get; init; }
    public bool? IsDefault { get; init; }
    public bool? IsActive { get; init; }
}

public record CreateApiKeyRequest
{
    public string Name { get; init; } = string.Empty;
    public DateTime? ExpiresAt { get; init; }
}