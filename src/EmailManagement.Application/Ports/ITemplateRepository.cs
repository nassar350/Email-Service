using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface ITemplateRepository
{
    Task<EmailTemplate?> GetByKeyAsync(string templateKey, CancellationToken ct = default);
    Task<EmailTemplate?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken ct = default);
    Task<bool> ExistsByKeyAsync(string templateKey, Guid? excludeId = null, CancellationToken ct = default);
    Task AddAsync(EmailTemplate template, CancellationToken ct = default);
    Task UpdateAsync(EmailTemplate template, CancellationToken ct = default);
    Task DeleteAsync(EmailTemplate template, CancellationToken ct = default);
    Task<bool> HasEmailsAsync(Guid templateId, CancellationToken ct = default);
}