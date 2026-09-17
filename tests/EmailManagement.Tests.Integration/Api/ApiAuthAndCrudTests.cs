using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using EmailManagement.Tests.Integration.Api;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmailManagement.Tests.Integration.Api;

public class ApiAuthAndCrudTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _factory;

    public ApiAuthAndCrudTests(ApiFactory factory) => _factory = factory;

    private static HttpClient AuthorizedClient(ApiFactory factory, string rawKey)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", rawKey);
        return client;
    }

    [Fact]
    public async Task GetWithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/emails");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWithInvalidToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "ems_live_wrongkey");

        var response = await client.GetAsync("/api/v1/emails");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task FullFlow_CreateTemplateActivateProviderSendAndQuery()
    {
        var key = await _factory.CreateApiKeyAsync();
        var client = AuthorizedClient(_factory, key.RawKey);

        var createTemplateBody = new
        {
            templateKey = "it_welcome",
            name = "Welcome",
            subjectTemplate = "Hi {{name}}!",
            htmlBody = "<h1>Hello {{name}}</h1>",
            variables = new[]
            {
                new { variableKey = "name", description = "Name", isRequired = true },
            },
        };
        var createResponse = await client.PostAsJsonAsync("/api/v1/templates", createTemplateBody);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var templateDoc = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var templateId = templateDoc.RootElement.GetProperty("data").GetProperty("templateId").GetGuid();

        var activateResponse = await client.PutAsJsonAsync($"/api/v1/templates/{templateId}", new { status = "Active" });
        activateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var providerResponse = await client.PostAsJsonAsync("/api/v1/providers", new
        {
            providerType = "Mailjet",
            providerName = "It Provider",
            isDefault = true,
            configuration = "{\"fromEmail\":\"noreply@example.com\"}",
        });
        providerResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var sendResponse = await client.PostAsJsonAsync("/api/v1/emails/send", new
        {
            templateKey = "it_welcome",
            to = new[] { "bob@example.com" },
            variables = new Dictionary<string, string> { ["name"] = "Bob" },
        });
        sendResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var sendDoc = JsonDocument.Parse(await sendResponse.Content.ReadAsStringAsync());
        var emailId = sendDoc.RootElement.GetProperty("data").GetProperty("emailId").GetGuid();

        var getResponse = await client.GetAsync($"/api/v1/emails/{emailId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var emailDoc = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        emailDoc.RootElement.GetProperty("data").GetProperty("subject").GetString().Should().Be("Hi Bob!");
        emailDoc.RootElement.GetProperty("data").GetProperty("status").GetInt32().Should().Be(0); // Queued

        // The same template key cannot be created twice.
        var duplicateResponse = await client.PostAsJsonAsync("/api/v1/templates", createTemplateBody);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Validation is enforced for required variables.
        var badSend = await client.PostAsJsonAsync("/api/v1/emails/send", new
        {
            templateKey = "it_welcome",
            to = new[] { "bob@example.com" },
            variables = new Dictionary<string, string>(),
        });
        badSend.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WebhookEndpoint_WithOpenAuth_ProcessesUnknownEventsGracefully()
    {
        var client = _factory.CreateClient();
        var body = """[ { "event": "open", "time": 1700000000, "MessageID": 999, "email": "x@example.com" } ]""";

        var response = await client.PostAsync("/api/v1/webhooks/mailjet",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("processed");
    }

    [Fact]
    public async Task WebhookEndpoint_EmptyBody_ReturnsBadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/v1/webhooks/mailjet",
            new StringContent("", Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}