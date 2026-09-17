using System.Security.Claims;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Exceptions;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace EmailManagement.Application.Services;

public class ApiKeyService
{
    private readonly IApiKeyRepository _repository;
    private readonly IApiKeyHasher _hasher;
    private readonly ILogger<ApiKeyService> _logger;

    public const string KeyPrefix = "ems_live_";
    public const int KeyEntropyBytes = 32;

    public ApiKeyService(IApiKeyRepository repository, IApiKeyHasher hasher, ILogger<ApiKeyService> logger)
    {
        _repository = repository;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task<ApiKeyCreatedResponse> GenerateAsync(string name, DateTime? expiresAt, CancellationToken ct = default)
    {
        var rawKey = KeyPrefix + Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(KeyEntropyBytes))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        var prefix = rawKey[..Math.Min(16, rawKey.Length)];
        var hash = _hasher.Hash(rawKey, prefix);

        var apiKey = new ApiKey
        {
            ApiKeyId = Guid.NewGuid(),
            Name = name,
            KeyPrefix = prefix,
            KeyHash = hash,
            IsActive = true,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
        };

        await _repository.AddAsync(apiKey, ct);

        _logger.LogInformation("API key created: {Name}, prefix: {Prefix}", name, prefix);

        return new ApiKeyCreatedResponse
        {
            ApiKeyId = apiKey.ApiKeyId,
            Name = apiKey.Name,
            RawKey = rawKey,
            KeyPrefix = apiKey.KeyPrefix,
            CreatedAt = apiKey.CreatedAt,
        };
    }

    public async Task<ClaimsPrincipal?> ValidateAsync(string rawKey, CancellationToken ct = default)
    {
        if (!rawKey.StartsWith(KeyPrefix))
            return null;

        var prefix = rawKey[..Math.Min(16, rawKey.Length)];
        var hash = _hasher.Hash(rawKey, prefix);
        var key = await _repository.GetByHashAsync(hash, ct);

        if (key is null)
            return null;

        if (!key.IsActive)
            return null;

        if (key.ExpiresAt.HasValue && key.ExpiresAt.Value < DateTime.UtcNow)
            return null;

        if (key.RevokedAt.HasValue)
            return null;

        var now = DateTime.UtcNow;
        if (key.LastUsedAt is null || (now - key.LastUsedAt.Value).TotalHours >= 1)
        {
            key.LastUsedAt = now;
            await _repository.UpdateAsync(key, ct);
        }

        var claims = new List<Claim>
        {
            new("api_key_id", key.ApiKeyId.ToString()),
            new(ClaimTypes.Name, key.Name),
            new("api_key_prefix", key.KeyPrefix),
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "ApiKey"));
    }

    public async Task<IReadOnlyList<ApiKeyResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var keys = await _repository.GetAllAsync(ct);
        return keys.Select(MapToResponse).ToList();
    }

    public async Task RevokeAsync(Guid id, CancellationToken ct = default)
    {
        var key = await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(ApiKey), id);

        key.IsActive = false;
        key.RevokedAt = DateTime.UtcNow;
        await _repository.UpdateAsync(key, ct);

        _logger.LogInformation("API key revoked: {Prefix}", key.KeyPrefix);
    }

    public async Task<ApiKeyCreatedResponse> BootstrapIfEmptyAsync(string? bootstrapKey = null, CancellationToken ct = default)
    {
        var count = await _repository.CountAsync(ct);
        if (count > 0)
            return null!;

        var name = bootstrapKey ?? "bootstrap";
        return await GenerateAsync(name, null, ct);
    }

    public static ApiKeyResponse MapToResponse(ApiKey key) => new()
    {
        ApiKeyId = key.ApiKeyId,
        Name = key.Name,
        KeyPrefix = key.KeyPrefix,
        IsActive = key.IsActive,
        ExpiresAt = key.ExpiresAt,
        CreatedAt = key.CreatedAt,
        RevokedAt = key.RevokedAt,
        LastUsedAt = key.LastUsedAt,
    };
}