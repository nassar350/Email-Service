using EmailManagement.Domain.Entities;

namespace EmailManagement.Application.Ports;

public interface ITemplateVersionRepository
{
    Task<TemplateVersion?> GetActiveAsync(Guid templateId, CancellationToken ct = default);
    Task<TemplateVersion?> GetByIdAsync(Guid versionId, CancellationToken ct = default);
    Task<IReadOnlyList<TemplateVersion>> GetByTemplateAsync(Guid templateId, CancellationToken ct = default);
    Task<int> GetMaxVersionNumberAsync(Guid templateId, CancellationToken ct = default);
    Task AddAsync(TemplateVersion version, CancellationToken ct = default);
    Task UpdateAsync(TemplateVersion version, CancellationToken ct = default);
}