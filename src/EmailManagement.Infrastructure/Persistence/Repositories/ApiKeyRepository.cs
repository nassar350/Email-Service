using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class ApiKeyRepository : IApiKeyRepository
{
    private readonly ApplicationDbContext _db;

    public ApiKeyRepository(ApplicationDbContext db) => _db = db;

    public async Task<ApiKey?> GetByHashAsync(string keyHash, CancellationToken ct = default) =>
        await _db.ApiKeys.FirstOrDefaultAsync(x => x.KeyHash == keyHash, ct);

    public async Task<ApiKey?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.ApiKeys.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<ApiKey>> GetAllAsync(CancellationToken ct = default) =>
        await _db.ApiKeys.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<int> CountAsync(CancellationToken ct = default) =>
        await _db.ApiKeys.CountAsync(ct);

    public async Task AddAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        _db.ApiKeys.Add(apiKey);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ApiKey apiKey, CancellationToken ct = default)
    {
        _db.ApiKeys.Update(apiKey);
        await _db.SaveChangesAsync(ct);
    }
}