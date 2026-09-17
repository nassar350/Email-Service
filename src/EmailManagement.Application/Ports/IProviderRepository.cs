using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IProviderRepository
{
    Task<EmailProvider?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmailProvider>> GetAllAsync(CancellationToken ct = default);
    Task<EmailProvider?> GetDefaultActiveAsync(ProviderType? providerType = null, CancellationToken ct = default);
    Task AddAsync(EmailProvider provider, CancellationToken ct = default);
    Task UpdateAsync(EmailProvider provider, CancellationToken ct = default);
    Task DeleteAsync(EmailProvider provider, CancellationToken ct = default);
    Task<bool> HasEmailsAsync(Guid providerId, CancellationToken ct = default);
}