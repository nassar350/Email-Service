using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class EmailEventRepository : IEmailEventRepository
{
    private readonly ApplicationDbContext _db;

    public EmailEventRepository(ApplicationDbContext db) => _db = db;

    public async Task<EmailEvent?> GetByProviderEventIdAsync(string providerEventId, CancellationToken ct = default) =>
        await _db.EmailEvents.FirstOrDefaultAsync(x => x.ProviderEventId == providerEventId, ct);

    public async Task<IReadOnlyList<EmailEvent>> GetByEmailAsync(Guid emailId, CancellationToken ct = default) =>
        await _db.EmailEvents
            .Include(x => x.Recipient)
            .Where(x => x.EmailId == emailId)
            .OrderByDescending(x => x.OccurredAt).ToListAsync(ct);

    public async Task AddAsync(EmailEvent emailEvent, CancellationToken ct = default)
    {
        _db.EmailEvents.Add(emailEvent);
    }

    public async Task AddRangeAsync(IEnumerable<EmailEvent> events, CancellationToken ct = default)
    {
        _db.EmailEvents.AddRange(events);
    }
}