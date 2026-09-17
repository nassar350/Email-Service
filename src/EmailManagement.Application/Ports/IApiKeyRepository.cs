using EmailManagement.Domain.Entities;

namespace EmailManagement.Application.Ports;

public interface IApiKeyRepository
{
    Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default);
    Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ApiKey>> GetAllAsync(CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
    Task AddAsync(ApiKey apiKey, CancellationToken ct = default);
    Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default);
}