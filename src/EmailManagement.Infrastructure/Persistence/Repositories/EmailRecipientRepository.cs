using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class EmailRecipientRepository : IEmailRecipientRepository
{
    private readonly ApplicationDbContext _db;

    public EmailRecipientRepository(ApplicationDbContext db) => _db = db;

    public Task AddRangeAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default)
    {
        _db.EmailRecipients.AddRange(recipients);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<EmailRecipient>> GetByEmailAsync(Guid emailId, CancellationToken ct = default) =>
        await _db.EmailRecipients.Where(x => x.EmailId == emailId).ToListAsync(ct);

    public async Task<EmailRecipient?> GetByEmailAndAddressAsync(Guid emailId, string email, CancellationToken ct = default) =>
        await _db.EmailRecipients.FirstOrDefaultAsync(x => x.EmailId == emailId && x.Email == email, ct);

    public Task UpdateAsync(EmailRecipient recipient, CancellationToken ct = default)
    {
        _db.EmailRecipients.Update(recipient);
        return Task.CompletedTask;
    }
}