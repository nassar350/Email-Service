using System.Net;
using System.Text;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.Enums;
using EmailManagement.Infrastructure.Options;
using EmailManagement.Infrastructure.Providers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace EmailManagement.Tests.Unit.Infrastructure;

public class MailjetEmailProviderTests
{
    private static EmailMessage CreateMessage() => new()
    {
        EmailId = Guid.NewGuid(),
        Subject = "Test",
        FromEmail = "sender@example.com",
        FromName = "Sender",
        Recipients =
        [
            new EmailRecipientAddress { Email = "to@example.com", RecipientType = RecipientType.To },
            new EmailRecipientAddress { Email = "cc@example.com", RecipientType = RecipientType.Cc },
            new EmailRecipientAddress { Email = "bcc@example.com", RecipientType = RecipientType.Bcc },
        ],
        HtmlBody = "<h1>Hi</h1>",
        TextBody = "Hi",
    };

    private static MailjetEmailProvider CreateProvider(
        Queue<HttpResponseMessage> responses,
        List<string>? requestBodies = null,
        MailjetOptions? options = null)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken ct) =>
            {
                if (requestBodies is not null && request.Content is not null)
                    requestBodies.Add(await request.Content.ReadAsStringAsync(ct));
                return responses.Dequeue();
            });

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("mailjet"))
            .Returns(() => new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.mailjet.com") });

        return new MailjetEmailProvider(factory.Object, Options.Create(options ?? new MailjetOptions()), NullLogger<MailjetEmailProvider>.Instance);
    }

    [Fact]
    public async Task SendAsync_Success_ReturnsMessageId()
    {
        var successJson = """
        { "Messages": [ { "Status": "success", "To": [ { "Email": "a@example.com", "MessageID": 1234567890, "Status": "success" } ] } ] }
        """;
        var provider = CreateProvider(new Queue<HttpResponseMessage>(
            new[] { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(successJson, Encoding.UTF8, "application/json") } }));

        var result = await provider.SendAsync(CreateMessage(), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ProviderMessageId.Should().Be("1234567890");
    }

    [Fact]
    public async Task SendAsync_SuccessWithoutMessageId_ReturnsFailure()
    {
        var emptyJson = """{ "Messages": [ { "Status": "success" } ] }""";
        var provider = CreateProvider(new Queue<HttpResponseMessage>(
            new[] { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(emptyJson, Encoding.UTF8, "application/json") } }));

        var result = await provider.SendAsync(CreateMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.IsPermanentFailure.Should().BeFalse();
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.BadRequest)]
    public async Task SendAsync_ClientErrors_ArePermanentFailures(HttpStatusCode statusCode)
    {
        var provider = CreateProvider(new Queue<HttpResponseMessage>(
            new[] { new HttpResponseMessage(statusCode) }));

        var result = await provider.SendAsync(CreateMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.IsPermanentFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain(statusCode.ToString());
    }

    [Fact]
    public async Task SendAsync_BadRequest_IncludesMailjetErrorBodyInResult()
    {
        var errorJson = """{ "Messages": [ { "Errors": [ { "ErrorCode": "903", "ErrorMessage": "Sender not permitted" } ] } ] }""";
        var provider = CreateProvider(new Queue<HttpResponseMessage>(
            new[] { new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(errorJson, Encoding.UTF8, "application/json"),
            } }));

        var result = await provider.SendAsync(CreateMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.IsPermanentFailure.Should().BeTrue();
        result.ErrorMessage.Should().Contain("903");
        result.ErrorMessage.Should().Contain("Sender not permitted");
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    [InlineData(HttpStatusCode.TooManyRequests)]
    public async Task SendAsync_ServerErrors_AreTransientFailures(HttpStatusCode statusCode)
    {
        var provider = CreateProvider(new Queue<HttpResponseMessage>(
            new[] { new HttpResponseMessage(statusCode) }));

        var result = await provider.SendAsync(CreateMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.IsPermanentFailure.Should().BeFalse();
    }

    [Fact]
    public async Task SendAsync_ThrowingHandler_ReturnsTransientFailure()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("boom"));

        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient("mailjet"))
            .Returns(() => new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.mailjet.com") });

        var provider = new MailjetEmailProvider(factory.Object, Options.Create(new MailjetOptions()), NullLogger<MailjetEmailProvider>.Instance);

        var result = await provider.SendAsync(CreateMessage(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.IsPermanentFailure.Should().BeFalse();
        result.ErrorMessage.Should().Contain("boom");
    }

    [Fact]
    public async Task SendAsync_Payload_WrapsInMessagesArray()
    {
        var responseJson = """{ "Messages": [ { "Status": "success", "To": [ { "MessageID": 99 } ] } ] }""";
        var bodies = new List<string>();
        var provider = CreateProvider(new Queue<HttpResponseMessage>(
            new[] { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") } }), bodies);

        var message = CreateMessage();
        await provider.SendAsync(message, CancellationToken.None);

        using var doc = System.Text.Json.JsonDocument.Parse(bodies[0]);
        var root = doc.RootElement;
        var msg = root.GetProperty("Messages")[0];
        msg.GetProperty("To")[0].GetProperty("Email").GetString().Should().Be("to@example.com");
        msg.GetProperty("Cc")[0].GetProperty("Email").GetString().Should().Be("cc@example.com");
        msg.GetProperty("Bcc")[0].GetProperty("Email").GetString().Should().Be("bcc@example.com");
        msg.GetProperty("CustomID").GetString().Should().Be(message.EmailId.ToString());
        msg.GetProperty("Subject").GetString().Should().Be("Test");
    }

    [Fact]
    public async Task SendAsync_FallsBackToConfiguredFromEmail_WhenMessageFromIsEmpty()
    {
        var responseJson = """{ "Messages": [ { "Status": "success", "To": [ { "MessageID": 99 } ] } ] }""";
        var bodies = new List<string>();
        var options = new MailjetOptions { FromEmail = "fallback@example.com", FromName = "Fallback" };
        var provider = CreateProvider(new Queue<HttpResponseMessage>(
            new[] { new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseJson, Encoding.UTF8, "application/json") } }), bodies, options);

        var message = CreateMessage();
        message.FromEmail = "";
        message.FromName = null;
        await provider.SendAsync(message, CancellationToken.None);

        using var doc = System.Text.Json.JsonDocument.Parse(bodies[0]);
        var from = doc.RootElement.GetProperty("Messages")[0].GetProperty("From");
        from.GetProperty("Email").GetString().Should().Be("fallback@example.com");
        from.GetProperty("Name").GetString().Should().Be("Fallback");
    }
}
