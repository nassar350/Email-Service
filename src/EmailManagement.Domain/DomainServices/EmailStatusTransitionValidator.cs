using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.DomainServices;

public static class EmailStatusTransitionValidator
{
    private static readonly IReadOnlyDictionary<EmailStatus, IReadOnlySet<EmailStatus>> WorkerTransitions =
        new Dictionary<EmailStatus, IReadOnlySet<EmailStatus>>
        {
            [EmailStatus.Queued] = new HashSet<EmailStatus> { EmailStatus.Processing },
            [EmailStatus.Processing] = new HashSet<EmailStatus>
                { EmailStatus.Sent, EmailStatus.Retrying, EmailStatus.Failed },
            [EmailStatus.Retrying] = new HashSet<EmailStatus>
                { EmailStatus.Processing, EmailStatus.Failed },
        };

    public static int Precedence(EmailStatus status) => status switch
    {
        EmailStatus.Queued => 0,
        EmailStatus.Processing => 1,
        EmailStatus.Retrying => 2,
        EmailStatus.Sent => 3,
        EmailStatus.Opened => 4,
        EmailStatus.Clicked => 5,
        EmailStatus.Delivered => 6,
        EmailStatus.Bounced => 7,
        EmailStatus.Blocked => 8,
        EmailStatus.Spam => 9,
        EmailStatus.Unsubscribed => 10,
        EmailStatus.Failed => 11,
        _ => -1,
    };

    public static bool CanWorkerTransition(EmailStatus from, EmailStatus to)
    {
        return WorkerTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
    }

    public static bool CanWebhookTransition(EmailStatus current, EmailStatus incoming)
    {
        if (current == EmailStatus.Failed)
            return false;

        if (incoming is EmailStatus.Failed or EmailStatus.Retrying)
            return false;

        return Precedence(incoming) >= Precedence(current);
    }

    public static EmailStatus ResolveWebhookStatus(EmailStatus current, EmailStatus incoming)
    {
        if (!CanWebhookTransition(current, incoming))
            return current;

        return incoming;
    }
}