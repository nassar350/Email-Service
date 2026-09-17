using EmailManagement.Application.Ports;
using EmailManagement.Domain.DomainServices;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using Microsoft.Extensions.Logging;
using DomainLogLevel = EmailManagement.Domain.Enums.LogLevel;

namespace EmailManagement.Application.Services;

public class WebhookEvent
{
    public string? ProviderEventId { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? RecipientEmail { get; set; }
    public EmailEventType EventType { get; set; }
    public string? RawEventData { get; set; }
    public DateTime OccurredAt { get; set; }
}

public class EmailWebhookService
{
    private readonly IEmailRepository _emailRepo;
    private readonly IEmailRecipientRepository _recipientRepo;
    private readonly IEmailEventRepository _eventRepo;
    private readonly IEmailLogRepository _logRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailWebhookService> _logger;

    public EmailWebhookService(
        IEmailRepository emailRepo,
        IEmailRecipientRepository recipientRepo,
        IEmailEventRepository eventRepo,
        IEmailLogRepository logRepo,
        IUnitOfWork unitOfWork,
        ILogger<EmailWebhookService> logger)
    {
        _emailRepo = emailRepo;
        _recipientRepo = recipientRepo;
        _eventRepo = eventRepo;
        _logRepo = logRepo;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessEventsAsync(IReadOnlyList<WebhookEvent> events, CancellationToken ct = default)
    {
        foreach (var webhookEvent in events)
        {
            await ProcessSingleEventAsync(webhookEvent, ct);
        }
    }

    private async Task ProcessSingleEventAsync(WebhookEvent webhookEvent, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(webhookEvent.ProviderEventId))
        {
            var existing = await _eventRepo.GetByProviderEventIdAsync(webhookEvent.ProviderEventId, ct);
            if (existing is not null)
            {
                _logger.LogDebug("Duplicate webhook event ignored: {ProviderEventId}", webhookEvent.ProviderEventId);
                return;
            }
        }

        if (string.IsNullOrEmpty(webhookEvent.ProviderMessageId))
        {
            _logger.LogWarning("Webhook event has no message ID, skipping");
            return;
        }

        var email = await _emailRepo.GetByMessageIdAsync(webhookEvent.ProviderMessageId, ct);
        if (email is null)
        {
            _logger.LogWarning("No email found for message ID: {MessageId}", webhookEvent.ProviderMessageId);
            return;
        }

        var newStatus = EmailStatusTransitionValidator.ResolveWebhookStatus(
            email.Status, webhookEvent.EventType.ToEmailStatus());

        if (newStatus == email.Status && email.Status != EmailStatus.Queued)
        {
            _logger.LogDebug("Webhook event {Event} does not change email {EmailId} status from {Current}",
                webhookEvent.EventType, email.EmailId, email.Status);
        }
        else
        {
            email.Status = newStatus;
            await _emailRepo.UpdateAsync(email, ct);
        }

        EmailRecipient? recipient = null;
        if (!string.IsNullOrEmpty(webhookEvent.RecipientEmail))
        {
            recipient = await _recipientRepo.GetByEmailAndAddressAsync(
                email.EmailId, webhookEvent.RecipientEmail, ct);
        }

        if (recipient is not null)
        {
            ApplyRecipientUpdate(recipient, webhookEvent);
            await _recipientRepo.UpdateAsync(recipient, ct);
        }

        await _eventRepo.AddAsync(new EmailEvent
        {
            EventId = Guid.NewGuid(),
            EmailId = email.EmailId,
            RecipientId = recipient?.RecipientId,
            EventType = webhookEvent.EventType,
            EventData = SanitizeEventData(webhookEvent.RawEventData),
            ProviderEventId = webhookEvent.ProviderEventId,
            OccurredAt = webhookEvent.OccurredAt,
        }, ct);

        await _logRepo.AddAsync(new EmailLog
        {
            LogId = Guid.NewGuid(),
            EmailId = email.EmailId,
            Level = DomainLogLevel.Information,
            Message = $"Webhook event processed: {webhookEvent.EventType}",
            CreatedAt = DateTime.UtcNow,
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Webhook event processed: {Event} for email {EmailId}",
            webhookEvent.EventType, email.EmailId);
    }

    private static void ApplyRecipientUpdate(EmailRecipient recipient, WebhookEvent webhookEvent)
    {
        switch (webhookEvent.EventType)
        {
            case EmailEventType.Delivered:
                recipient.Status = EmailStatus.Delivered;
                recipient.DeliveredAt = webhookEvent.OccurredAt;
                break;
            case EmailEventType.Opened:
                recipient.Status = EmailStatus.Opened;
                recipient.OpenedAt ??= webhookEvent.OccurredAt;
                break;
            case EmailEventType.Clicked:
                recipient.Status = EmailStatus.Clicked;
                recipient.ClickedAt ??= webhookEvent.OccurredAt;
                break;
            case EmailEventType.Bounced:
                recipient.Status = EmailStatus.Bounced;
                break;
            case EmailEventType.Blocked:
                recipient.Status = EmailStatus.Blocked;
                break;
            case EmailEventType.Spam:
                recipient.Status = EmailStatus.Spam;
                break;
            case EmailEventType.Unsubscribed:
                recipient.Status = EmailStatus.Unsubscribed;
                break;
        }
    }

    private static string? SanitizeEventData(string? eventData)
    {
        if (eventData is null) return null;
        if (eventData.Length > 4000)
            eventData = eventData[..4000];
        return eventData;
    }
}

public static class EmailEventTypeExtensions
{
    public static EmailStatus ToEmailStatus(this EmailEventType eventType) => eventType switch
    {
        EmailEventType.Queued => EmailStatus.Queued,
        EmailEventType.Processing => EmailStatus.Processing,
        EmailEventType.Sent => EmailStatus.Sent,
        EmailEventType.Delivered => EmailStatus.Delivered,
        EmailEventType.Bounced => EmailStatus.Bounced,
        EmailEventType.Blocked => EmailStatus.Blocked,
        EmailEventType.Opened => EmailStatus.Opened,
        EmailEventType.Clicked => EmailStatus.Clicked,
        EmailEventType.Spam => EmailStatus.Spam,
        EmailEventType.Unsubscribed => EmailStatus.Unsubscribed,
        EmailEventType.Failed => EmailStatus.Failed,
        _ => EmailStatus.Queued,
    };
}