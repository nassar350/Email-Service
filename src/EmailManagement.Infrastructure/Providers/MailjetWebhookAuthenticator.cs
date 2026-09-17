using EmailManagement.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailManagement.Infrastructure.Providers;

public class MailjetWebhookAuthenticator
{
    private readonly MailjetOptions _options;
    private readonly ILogger<MailjetWebhookAuthenticator> _logger;

    public MailjetWebhookAuthenticator(IOptions<MailjetOptions> options, ILogger<MailjetWebhookAuthenticator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public bool Authenticate(string? authorizationHeader)
    {
        if (string.IsNullOrEmpty(_options.WebhookUsername) &&
            string.IsNullOrEmpty(_options.WebhookPassword) &&
            string.IsNullOrEmpty(_options.WebhookToken))
            return true;

        if (string.IsNullOrEmpty(authorizationHeader))
        {
            _logger.LogWarning("Webhook authentication failed: missing Authorization header");
            return false;
        }

        if (!string.IsNullOrEmpty(_options.WebhookToken))
        {
            var token = authorizationHeader.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);
            if (token == _options.WebhookToken)
                return true;
        }

        if (!string.IsNullOrEmpty(_options.WebhookUsername) && !string.IsNullOrEmpty(_options.WebhookPassword))
        {
            if (authorizationHeader.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                var decoded = System.Text.Encoding.UTF8.GetString(
                    Convert.FromBase64String(authorizationHeader["Basic ".Length..]));
                var parts = decoded.Split(':', 2);
                if (parts.Length == 2 && parts[0] == _options.WebhookUsername && parts[1] == _options.WebhookPassword)
                    return true;
            }
        }

        _logger.LogWarning("Webhook authentication failed");
        return false;
    }
}