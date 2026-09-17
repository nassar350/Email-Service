using EmailManagement.Application.Ports;
using EmailManagement.Application.Services;
using EmailManagement.Domain.Enums;
using EmailManagement.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmailManagement.Tests.Integration;

public class EmailProcessingServiceTests
{
    private static TestHost CreateHost(IEmailProvider? providerOverride)
    {
        return TestHost.Create(services =>
        {
            services.RemoveAll<IEmailProviderFactory>();
            services.AddSingleton<IEmailProviderFactory>(new FakeProviderFactory(providerOverride!));
        });
    }

    private static async Task<Guid> QueueEmailAsync(IServiceProvider sp, string key = "welcome")
    {
        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var template = await SeedData.CreateTemplateAsync(db, key: key);
        await SeedData.CreateProviderAsync(db);
        var enqueue = sp.GetRequiredService<IEmailEnqueueService>();
        var email = await enqueue.EnqueueAsync(
            template.TemplateKey,
            new[] { "a@example.com" },
            Array.Empty<string>(),
            Array.Empty<string>(),
            new Dictionary<string, string> { ["name"] = "Alice" });
        return email.EmailId;
    }

    [Fact]
    public async Task ProcessBatchAsync_TransientFailure_SchedulesRetry()
    {
        using var host = CreateHost(new FakeProvider(EmailSendResult.Failed("temporary outage", permanent: false)));
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var emailId = await QueueEmailAsync(sp);

        var processing = sp.GetRequiredService<EmailProcessingService>();
        await processing.ProcessBatchAsync(1);

        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var stored = await db.Emails.SingleAsync(e => e.EmailId == emailId);
        stored.Status.Should().Be(EmailStatus.Retrying);
        stored.RetryCount.Should().Be(1);
        stored.NextRetryAt.Should().NotBeNull();
        stored.LastError.Should().Contain("temporary outage");

        (await db.EmailEvents.AnyAsync(e => e.EmailId == emailId && e.EventType == EmailEventType.Failed)).Should().BeTrue();
        (await db.EmailLogs.AnyAsync(l => l.EmailId == emailId && l.Level == LogLevel.Warning)).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessBatchAsync_PermanentFailure_MarksFailed()
    {
        using var host = CreateHost(new FakeProvider(EmailSendResult.Failed("invalid api key", permanent: true)));
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var emailId = await QueueEmailAsync(sp, key: "perm");

        var processing = sp.GetRequiredService<EmailProcessingService>();
        await processing.ProcessBatchAsync(1);

        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var stored = await db.Emails.SingleAsync(e => e.EmailId == emailId);
        stored.Status.Should().Be(EmailStatus.Failed);
        stored.RetryCount.Should().Be(1);
        stored.NextRetryAt.Should().BeNull();
        stored.LastError.Should().Contain("invalid api key");

        (await db.EmailEvents.AnyAsync(e => e.EmailId == emailId && e.EventType == EmailEventType.Failed)).Should().BeTrue();
        (await db.EmailLogs.AnyAsync(l => l.EmailId == emailId && l.Level == LogLevel.Error)).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessBatchAsync_Success_MarksSent()
    {
        using var host = CreateHost(new FakeProvider(EmailSendResult.Succeeded("msg-123")));
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var emailId = await QueueEmailAsync(sp, key: "success");

        var processing = sp.GetRequiredService<EmailProcessingService>();
        await processing.ProcessBatchAsync(1);

        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var stored = await db.Emails.SingleAsync(e => e.EmailId == emailId);
        stored.Status.Should().Be(EmailStatus.Sent);
        stored.MessageId.Should().Be("msg-123");
        stored.SentAt.Should().NotBeNull();
        stored.RetryCount.Should().Be(0);

        (await db.EmailEvents.AnyAsync(e => e.EmailId == emailId && e.EventType == EmailEventType.Sent)).Should().BeTrue();
        (await db.EmailLogs.AnyAsync(l => l.EmailId == emailId && l.Message!.Contains("msg-123"))).Should().BeTrue();
    }

    private sealed class FakeProviderFactory(IEmailProvider provider) : IEmailProviderFactory
    {
        public IEmailProvider GetProvider(ProviderType providerType) => provider;
    }

    private sealed class FakeProvider : IEmailProvider
    {
        private readonly EmailSendResult _result;

        public FakeProvider(EmailSendResult result) => _result = result;

        public string ProviderType => "Mailjet";

        public Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken) =>
            Task.FromResult(_result);
    }
}