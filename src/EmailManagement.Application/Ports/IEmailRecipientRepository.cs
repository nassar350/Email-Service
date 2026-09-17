using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IEmailRecipientRepository
{
    Task AddRangeAsync(IEnumerable<EmailRecipient> recipients, CancellationToken ct = default);
    Task<IReadOnlyList<EmailRecipient>> GetByEmailAsync(Guid emailId, CancellationToken ct = default);
    Task<EmailRecipient?> GetByEmailAndAddressAsync(Guid emailId, string email, CancellationToken ct = default);
    Task UpdateAsync(EmailRecipient recipient, CancellationToken ct = default);
}