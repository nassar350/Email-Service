using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IEmailEnqueueService
{
    Task<Email> EnqueueAsync(
        string templateKey,
        IReadOnlyList<string> to,
        IReadOnlyList<string> cc,
        IReadOnlyList<string> bcc,
        Dictionary<string, string> variables,
        CancellationToken ct = default);
}