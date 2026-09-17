using System.Text;
using System.Text.Json;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using EmailManagement.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailManagement.Infrastructure.Providers;

public class MailjetEmailProvider : IEmailProvider, IEmailProviderDiagnostics
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MailjetOptions _options;
    private readonly ILogger<MailjetEmailProvider> _logger;

    public string ProviderType => "Mailjet";

    public MailjetEmailProvider(
        IHttpClientFactory httpClientFactory,
        IOptions<MailjetOptions> options,
        ILogger<MailjetEmailProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("mailjet");

            var fromEmail = string.IsNullOrWhiteSpace(message.FromEmail) ? _options.FromEmail : message.FromEmail;
            var fromName = string.IsNullOrWhiteSpace(message.FromName) ? _options.FromName : message.FromName;

            var payload = new
            {
                Messages = new[]
                {
                    new
                    {
                        From = new { Email = fromEmail, Name = fromName ?? "" },
                        To = message.Recipients
                            .Where(r => r.RecipientType == RecipientType.To)
                            .Select(r => new { Email = r.Email, Name = r.Name ?? "" }).ToList(),
                        Cc = message.Recipients
                            .Where(r => r.RecipientType == RecipientType.Cc)
                            .Select(r => new { Email = r.Email, Name = r.Name ?? "" }).ToList(),
                        Bcc = message.Recipients
                            .Where(r => r.RecipientType == RecipientType.Bcc)
                            .Select(r => new { Email = r.Email, Name = r.Name ?? "" }).ToList(),
                        Subject = message.Subject,
                        TextPart = message.TextBody ?? "",
                        HTMLPart = message.HtmlBody ?? "",
                        CustomID = message.EmailId.ToString(),
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("/v3.1/send", content, cancellationToken);

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<MailjetSendResponse>(responseJson);
                var messageId = result?.Messages?.FirstOrDefault()?.To?.FirstOrDefault()?.MessageID?.ToString();

                if (!string.IsNullOrEmpty(messageId))
                    return EmailSendResult.Succeeded(messageId);

                return EmailSendResult.Failed("Message accepted but no MessageID returned");
            }

            if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or
                System.Net.HttpStatusCode.Forbidden or
                System.Net.HttpStatusCode.BadRequest)
            {
                var errorDetail = SummarizeError(responseJson);
                _logger.LogWarning("Mailjet permanent failure for email {EmailId}: {StatusCode}. {Error}",
                    message.EmailId, response.StatusCode, errorDetail);
                return EmailSendResult.Failed($"Provider error: {response.StatusCode}. {errorDetail}", permanent: true);
            }

            var transientDetail = SummarizeError(responseJson);
            _logger.LogWarning("Mailjet transient failure for email {EmailId}: {StatusCode}. {Error}",
                message.EmailId, response.StatusCode, transientDetail);
            return EmailSendResult.Failed($"Provider error: {response.StatusCode}. {transientDetail}");
        }
        catch (TaskCanceledException)
        {
            return EmailSendResult.Failed("Request timed out");
        }
        catch (HttpRequestException ex)
        {
            return EmailSendResult.Failed($"HTTP error: {ex.Message}");
        }
    }

    private static string SummarizeError(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson))
            return string.Empty;

        var cleaned = responseJson
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");

        return cleaned.Length > 300 ? cleaned[..300] : cleaned;
    }

    public async Task<ProviderTestResult> TestAsync(CancellationToken ct = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("mailjet");
            var response = await client.GetAsync("/v3/REST/contact", ct);

            if (response.IsSuccessStatusCode)
                return ProviderTestResult.Ok("Mailjet API is reachable and credentials are valid.");

            return ProviderTestResult.Failed($"Mailjet API returned: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            return ProviderTestResult.Failed($"Cannot reach Mailjet API: {ex.Message}");
        }
    }
}

public class MailjetSendResponse
{
    public List<MailjetMessage>? Messages { get; set; }
}

public class MailjetMessage
{
    public string? Status { get; set; }
    public List<MailjetRecipient>? To { get; set; }
    public List<MailjetError>? Errors { get; set; }
}

public class MailjetRecipient
{
    public string? Email { get; set; }
    public long? MessageID { get; set; }
    public string? Status { get; set; }
}

public class MailjetError
{
    public string? Status { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
}
