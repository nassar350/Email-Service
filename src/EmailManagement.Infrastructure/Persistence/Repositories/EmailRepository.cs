using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class EmailRepository : IEmailRepository
{
    private readonly ApplicationDbContext _db;

    public EmailRepository(ApplicationDbContext db) => _db = db;

    public async Task<Email?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _db.Emails
            .Include(x => x.Recipients)
            .Include(x => x.Template)
            .Include(x => x.Version)
            .Include(x => x.Provider)
            .FirstOrDefaultAsync(x => x.EmailId == id, ct);

    public async Task<Email?> GetByMessageIdAsync(string messageId, CancellationToken ct = default) =>
        await _db.Emails
            .Include(x => x.Recipients)
            .FirstOrDefaultAsync(x => x.MessageId == messageId, ct);

    public async Task<(IReadOnlyList<Email> Items, int TotalCount)> QueryAsync(
        EmailStatus? status,
        string? templateKey,
        string? recipientEmail,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = _db.Emails
            .Include(x => x.Recipients)
            .Include(x => x.Template)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (!string.IsNullOrEmpty(templateKey))
            query = query.Where(x => x.Template.TemplateKey == templateKey);

        if (!string.IsNullOrEmpty(recipientEmail))
            query = query.Where(x => x.Recipients.Any(r => r.Email == recipientEmail));

        if (dateFrom.HasValue)
            query = query.Where(x => x.CreatedAt >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(x => x.CreatedAt <= dateTo.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }

    public async Task<IReadOnlyList<Email>> GetEligibleForProcessingAsync(int batchSize, CancellationToken ct = default) =>
        await _db.Emails
            .Include(x => x.Recipients)
            .Include(x => x.Provider)
            .Include(x => x.Version)
            .Where(x =>
                (x.Status == EmailStatus.Queued ||
                 (x.Status == EmailStatus.Retrying && x.NextRetryAt <= DateTime.UtcNow)))
            .OrderBy(x => x.QueuedAt)
            .Take(batchSize)
            .ToListAsync(ct);

    public async Task<bool> ClaimForProcessingAsync(Guid emailId, CancellationToken ct = default)
    {
        var affected = await _db.Database.ExecuteSqlRawAsync(
            "UPDATE Emails SET Status = 'Processing', ProcessingAt = {0} WHERE EmailId = {1} AND Status IN ('Queued', 'Retrying')",
            new object[] { DateTime.UtcNow, emailId },
            ct);

        return affected > 0;
    }

    public async Task AddAsync(Email email, CancellationToken ct = default)
    {
        _db.Emails.Add(email);
    }

    public async Task UpdateAsync(Email email, CancellationToken ct = default)
    {
        _db.Emails.Update(email);
    }
}