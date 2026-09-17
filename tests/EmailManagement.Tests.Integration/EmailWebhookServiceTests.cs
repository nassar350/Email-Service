using EmailManagement.Application.Ports;
using EmailManagement.Application.Services;
using EmailManagement.Domain.Enums;
using EmailManagement.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmailManagement.Tests.Integration;

public class EmailWebhookServiceTests
{
    private static async Task<Guid> SeedSentEmailAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var template = await SeedData.CreateTemplateAsync(db);
        var provider = await SeedData.CreateProviderAsync(db);
        var enqueue = sp.GetRequiredService<IEmailEnqueueService>();
        var email = await enqueue.EnqueueAsync(
            template.TemplateKey,
            new[] { "a@example.com" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            new Dictionary<string, string> { ["name"] = "Alice" });

        email.Status = EmailStatus.Sent;
        email.MessageId = "mailjet-msg-1";
        db.Emails.Update(email);
        await db.SaveChangesAsync();

        return email.EmailId;
    }

    [Fact]
    public async Task ProcessEventsAsync_DeliveredEvent_UpdatesStatusAndRecipient()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var emailId = await SeedSentEmailAsync(sp);
        var service = sp.GetRequiredService<EmailWebhookService>();

        await service.ProcessEventsAsync(new[]
        {
            new WebhookEvent
            {
                ProviderEventId = "evt-1",
                ProviderMessageId = "mailjet-msg-1",
                RecipientEmail = "a@example.com",
                EventType = EmailEventType.Delivered,
                OccurredAt = DateTime.UtcNow,
            },
        });

        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var email = await db.Emails.SingleAsync(e => e.EmailId == emailId);
        email.Status.Should().Be(EmailStatus.Delivered);

        var recipient = await db.EmailRecipients.SingleAsync(r => r.EmailId == emailId);
        recipient.Status.Should().Be(EmailStatus.Delivered);
        recipient.DeliveredAt.Should().NotBeNull();

        var events = await db.EmailEvents.Where(e => e.EmailId == emailId).ToListAsync();
        events.Should().Contain(e => e.EventType == EmailEventType.Delivered && e.ProviderEventId == "evt-1");
    }

    [Fact]
    public async Task ProcessEventsAsync_DuplicateEventId_IsIgnored()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var emailId = await SeedSentEmailAsync(sp);
        var service = sp.GetRequiredService<EmailWebhookService>();

        var webhookEvent = new WebhookEvent
        {
            ProviderEventId = "evt-dup",
            ProviderMessageId = "mailjet-msg-1",
            RecipientEmail = "a@example.com",
            EventType = EmailEventType.Delivered,
            OccurredAt = DateTime.UtcNow,
        };

        await service.ProcessEventsAsync(new[] { webhookEvent });
        await service.ProcessEventsAsync(new[] { webhookEvent });

        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var deliveredCount = await db.EmailEvents.CountAsync(e =>
            e.EmailId == emailId && e.EventType == EmailEventType.Delivered && e.ProviderEventId == "evt-dup");

        deliveredCount.Should().Be(1);
    }

    [Fact]
    public async Task ProcessEventsAsync_OutOfOrderEvent_DoesNotRegressStatus()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var emailId = await SeedSentEmailAsync(sp);
        var service = sp.GetRequiredService<EmailWebhookService>();

        // Delivered (higher precedence) arrives first, then an older Open event.
        await service.ProcessEventsAsync(new[]
        {
            new WebhookEvent { ProviderEventId = "d1", ProviderMessageId = "mailjet-msg-1", EventType = EmailEventType.Delivered, OccurredAt = DateTime.UtcNow },
        });
        await service.ProcessEventsAsync(new[]
        {
            new WebhookEvent { ProviderEventId = "d2", ProviderMessageId = "mailjet-msg-1", EventType = EmailEventType.Opened, OccurredAt = DateTime.UtcNow },
        });

        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var email = await db.Emails.SingleAsync(e => e.EmailId == emailId);
        email.Status.Should().Be(EmailStatus.Delivered);
    }

    [Fact]
    public async Task ProcessEventsAsync_UnknownMessageId_IsSkippedWithoutError()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<EmailWebhookService>();

        await service.ProcessEventsAsync(new[]
        {
            new WebhookEvent { ProviderEventId = "unknown", ProviderMessageId = "no-such-msg", EventType = EmailEventType.Sent, OccurredAt = DateTime.UtcNow },
        });
    }
}