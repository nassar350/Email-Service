using EmailManagement.Application.Exceptions;
using EmailManagement.Application.Ports;
using EmailManagement.Application.Services;
using EmailManagement.Domain.Enums;
using EmailManagement.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmailManagement.Tests.Integration;

public class EmailEnqueueServiceTests
{
    [Fact]
    public async Task EnqueueAsync_PersistsEmailRecipientsEventAndLog()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var template = await SeedData.CreateTemplateAsync(db);
        await SeedData.CreateProviderAsync(db);

        var service = scope.ServiceProvider.GetRequiredService<IEmailEnqueueService>();
        var email = await service.EnqueueAsync(
            template.TemplateKey,
            new[] { "a@example.com" },
            Array.Empty<string>(),
            new[] { "bcc@example.com" },
            new Dictionary<string, string> { ["name"] = "Alice" });

        email.EmailId.Should().NotBe(Guid.Empty);
        email.Status.Should().Be(EmailStatus.Queued);
        email.Subject.Should().Be("Hi Alice");

        var stored = await db.Emails
            .Include(e => e.Recipients)
            .SingleAsync(e => e.EmailId == email.EmailId);

        stored.Recipients.Should().HaveCount(2);
        stored.Recipients.Should().Contain(r => r.Email == "a@example.com" && r.RecipientType == RecipientType.To);
        stored.Recipients.Should().Contain(r => r.Email == "bcc@example.com" && r.RecipientType == RecipientType.Bcc);

        (await db.EmailEvents.CountAsync(e => e.EmailId == email.EmailId)).Should().Be(1);
        (await db.EmailLogs.CountAsync(l => l.EmailId == email.EmailId)).Should().Be(1);
    }

    [Fact]
    public async Task EnqueueAsync_ThrowsWhenTemplateNotFound()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IEmailEnqueueService>();

        var act = () => service.EnqueueAsync(
            "missing", new[] { "a@example.com" }, Array.Empty<string>(), Array.Empty<string>(),
            new Dictionary<string, string>());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task EnqueueAsync_ThrowsWhenTemplateNotActive()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var template = await SeedData.CreateTemplateAsync(db, status: TemplateStatus.Draft);

        var service = scope.ServiceProvider.GetRequiredService<IEmailEnqueueService>();
        var act = () => service.EnqueueAsync(
            template.TemplateKey, new[] { "a@example.com" }, Array.Empty<string>(), Array.Empty<string>(),
            new Dictionary<string, string> { ["name"] = "X" });

        await act.Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task EnqueueAsync_ThrowsWhenRequiredVariableMissing()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var template = await SeedData.CreateTemplateAsync(db);

        var service = scope.ServiceProvider.GetRequiredService<IEmailEnqueueService>();
        var act = () => service.EnqueueAsync(
            template.TemplateKey, new[] { "a@example.com" }, Array.Empty<string>(), Array.Empty<string>(),
            new Dictionary<string, string>());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task EnqueueAsync_ThrowsWhenNoActiveProvider()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();
        var template = await SeedData.CreateTemplateAsync(db);

        var service = scope.ServiceProvider.GetRequiredService<IEmailEnqueueService>();
        var act = () => service.EnqueueAsync(
            template.TemplateKey, new[] { "a@example.com" }, Array.Empty<string>(), Array.Empty<string>(),
            new Dictionary<string, string> { ["name"] = "X" });

        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*provider*");
    }
}