using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class ProviderRepository : IProviderRepository
{
    private readonly ApplicationDbContext _db;

    public ProviderRepository(ApplicationDbContext db) => _db = db;

    public async Task<EmailProvider?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.EmailProviders.FindAsync(new object[] { id }, ct);

    public async Task<IReadOnlyList<EmailProvider>> GetAllAsync(CancellationToken ct = default) =>
        await _db.EmailProviders.OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<EmailProvider?> GetDefaultActiveAsync(ProviderType? providerType = null, CancellationToken ct = default) =>
        await _db.EmailProviders.FirstOrDefaultAsync(x =>
            x.IsActive && x.IsDefault &&
            (providerType == null || x.ProviderType == providerType), ct);

    public async Task AddAsync(EmailProvider provider, CancellationToken ct = default)
    {
        _db.EmailProviders.Add(provider);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EmailProvider provider, CancellationToken ct = default)
    {
        _db.EmailProviders.Update(provider);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EmailProvider provider, CancellationToken ct = default)
    {
        _db.EmailProviders.Remove(provider);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> HasEmailsAsync(Guid providerId, CancellationToken ct = default) =>
        await _db.Emails.AnyAsync(x => x.ProviderId == providerId, ct);
}