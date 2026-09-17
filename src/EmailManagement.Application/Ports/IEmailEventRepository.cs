using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IEmailEventRepository
{
    Task<EmailEvent?> GetByProviderEventIdAsync(string providerEventId, CancellationToken ct = default);
    Task<IReadOnlyList<EmailEvent>> GetByEmailAsync(Guid emailId, CancellationToken ct = default);
    Task AddAsync(EmailEvent emailEvent, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<EmailEvent> events, CancellationToken ct = default);
}