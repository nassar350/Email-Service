using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IEmailLogRepository
{
    Task<IReadOnlyList<EmailLog>> GetByEmailAsync(Guid emailId, CancellationToken ct = default);
    Task AddAsync(EmailLog log, CancellationToken ct = default);
}