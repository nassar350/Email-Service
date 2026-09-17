using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence.Repositories;

public class EmailLogRepository : IEmailLogRepository
{
    private readonly ApplicationDbContext _db;

    public EmailLogRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<EmailLog>> GetByEmailAsync(Guid emailId, CancellationToken ct = default) =>
        await _db.EmailLogs
            .Where(x => x.EmailId == emailId)
            .OrderByDescending(x => x.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(EmailLog log, CancellationToken ct = default)
    {
        _db.EmailLogs.Add(log);
    }
}