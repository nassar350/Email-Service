using System.Text;
using EmailManagement.Infrastructure.Options;
using EmailManagement.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EmailManagement.Tests.Unit.Infrastructure;

public class MailjetWebhookAuthenticatorTests
{
    private static MailjetWebhookAuthenticator Create(MailjetOptions options) =>
        new(Options.Create(options), NullLogger<MailjetWebhookAuthenticator>.Instance);

    [Fact]
    public void Authenticate_NoCredsConfigured_Allows()
    {
        var auth = Create(new MailjetOptions());

        auth.Authenticate(null).Should().BeTrue();
    }

    [Fact]
    public void Authenticate_BearerToken_Matches()
    {
        var auth = Create(new MailjetOptions { WebhookToken = "wht-123" });

        auth.Authenticate("Bearer wht-123").Should().BeTrue();
    }

    [Fact]
    public void Authenticate_BearerToken_WrongRejects()
    {
        var auth = Create(new MailjetOptions { WebhookToken = "wht-123" });

        auth.Authenticate("Bearer wrong").Should().BeFalse();
    }

    [Fact]
    public void Authenticate_Basic_Matches()
    {
        var auth = Create(new MailjetOptions { WebhookUsername = "user", WebhookPassword = "pass" });
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:pass"));

        auth.Authenticate($"Basic {basic}").Should().BeTrue();
    }

    [Fact]
    public void Authenticate_Basic_WrongRejects()
    {
        var auth = Create(new MailjetOptions { WebhookUsername = "user", WebhookPassword = "pass" });
        var basic = Convert.ToBase64String(Encoding.UTF8.GetBytes("user:wrong"));

        auth.Authenticate($"Basic {basic}").Should().BeFalse();
    }

    [Fact]
    public void Authenticate_MissingHeaderRejects()
    {
        var auth = Create(new MailjetOptions { WebhookToken = "wht-123" });

        auth.Authenticate(null).Should().BeFalse();
    }
}