using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IEmailRepository
{
    Task<Email?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Email?> GetByMessageIdAsync(string messageId, CancellationToken ct = default);
    Task<(IReadOnlyList<Email> Items, int TotalCount)> QueryAsync(
        EmailStatus? status,
        string? templateKey,
        string? recipientEmail,
        DateTime? dateFrom,
        DateTime? dateTo,
        int page,
        int pageSize,
        CancellationToken ct = default);
    Task<IReadOnlyList<Email>> GetEligibleForProcessingAsync(int batchSize, CancellationToken ct = default);
    Task<bool> ClaimForProcessingAsync(Guid emailId, CancellationToken ct = default);
    Task AddAsync(Email email, CancellationToken ct = default);
    Task UpdateAsync(Email email, CancellationToken ct = default);
}