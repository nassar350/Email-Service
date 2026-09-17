using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class TemplateRepository : ITemplateRepository
{
    private readonly ApplicationDbContext _db;

    public TemplateRepository(ApplicationDbContext db) => _db = db;

    public async Task<EmailTemplate?> GetByKeyAsync(string templateKey, CancellationToken ct = default) =>
        await _db.EmailTemplates
            .Include(x => x.Variables)
            .FirstOrDefaultAsync(x => x.TemplateKey == templateKey, ct);

    public async Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.EmailTemplates
            .Include(x => x.Variables)
            .Include(x => x.Versions.Where(v => v.IsActive))
            .FirstOrDefaultAsync(x => x.TemplateId == id, ct);

    public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken ct = default) =>
        await _db.EmailTemplates
            .Include(x => x.Variables)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task<bool> ExistsByKeyAsync(string templateKey, Guid? excludeId = null, CancellationToken ct = default) =>
        await _db.EmailTemplates.AnyAsync(x => x.TemplateKey == templateKey && (excludeId == null || x.TemplateId != excludeId), ct);

    public async Task AddAsync(EmailTemplate template, CancellationToken ct = default)
    {
        _db.EmailTemplates.Add(template);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(EmailTemplate template, CancellationToken ct = default)
    {
        _db.EmailTemplates.Update(template);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EmailTemplate template, CancellationToken ct = default)
    {
        _db.EmailTemplates.Remove(template);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> HasEmailsAsync(Guid templateId, CancellationToken ct = default) =>
        await _db.Emails.AnyAsync(x => x.TemplateId == templateId, ct);
}