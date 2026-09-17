using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class TemplateVersionRepository : ITemplateVersionRepository
{
    private readonly ApplicationDbContext _db;

    public TemplateVersionRepository(ApplicationDbContext db) => _db = db;

    public async Task<TemplateVersion?> GetActiveAsync(Guid templateId, CancellationToken ct = default) =>
        await _db.TemplateVersions.FirstOrDefaultAsync(x => x.TemplateId == templateId && x.IsActive, ct);

    public async Task<TemplateVersion?> GetByIdAsync(Guid versionId, CancellationToken ct = default) =>
        await _db.TemplateVersions.FindAsync(new object[] { versionId }, ct);

    public async Task<IReadOnlyList<TemplateVersion>> GetByTemplateAsync(Guid templateId, CancellationToken ct = default) =>
        await _db.TemplateVersions
            .Where(x => x.TemplateId == templateId)
            .OrderByDescending(x => x.VersionNumber).ToListAsync(ct);

    public async Task<int> GetMaxVersionNumberAsync(Guid templateId, CancellationToken ct = default) =>
        await _db.TemplateVersions
            .Where(x => x.TemplateId == templateId)
            .MaxAsync(x => x.VersionNumber, ct);

    public async Task AddAsync(TemplateVersion version, CancellationToken ct = default)
    {
        _db.TemplateVersions.Add(version);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(TemplateVersion version, CancellationToken ct = default)
    {
        _db.TemplateVersions.Update(version);
        await _db.SaveChangesAsync(ct);
    }
}