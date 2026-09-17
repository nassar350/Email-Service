using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.DTOs.Responses;

public record ProviderResponse
{
    public Guid ProviderId { get; init; }
    public ProviderType ProviderType { get; init; }
    public string ProviderName { get; init; } = string.Empty;
    public string? Configuration { get; init; }
    public bool IsDefault { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public record ProviderTestResponse
{
    public bool Success { get; init; }
    public string? Message { get; init; }
}

public record ApiKeyResponse
{
    public Guid ApiKeyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? RevokedAt { get; init; }
    public DateTime? LastUsedAt { get; init; }
}

public record ApiKeyCreatedResponse
{
    public Guid ApiKeyId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RawKey { get; init; } = string.Empty;
    public string KeyPrefix { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record ApiResponse<T>
{
    public T? Data { get; init; }
    public string? Message { get; init; }
}