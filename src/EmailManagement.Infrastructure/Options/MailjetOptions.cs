namespace EmailManagement.Infrastructure.Options;

public class MailjetOptions
{
    public const string SectionName = "Mailjet";
    public string ApiKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string? WebhookUsername { get; set; }
    public string? WebhookPassword { get; set; }
    public string? WebhookToken { get; set; }
    public bool SkipStartupValidation { get; set; }
}