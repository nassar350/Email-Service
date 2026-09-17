using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface IEmailProvider
{
    string ProviderType { get; }
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}

public class EmailMessage
{
    public Guid EmailId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }
    public List<EmailRecipientAddress> Recipients { get; set; } = new();
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public List<EmailAttachmentData> Attachments { get; set; } = new();
    public Dictionary<string, string>? Headers { get; set; }
}

public class EmailRecipientAddress
{
    public string Email { get; set; } = string.Empty;
    public string? Name { get; set; }
    public RecipientType RecipientType { get; set; }
}

public class EmailAttachmentData
{
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public class EmailSendResult
{
    public bool Success { get; set; }
    public string? ProviderMessageId { get; set; }
    public bool IsPermanentFailure { get; set; }
    public string? ErrorMessage { get; set; }
    public List<string> RecipientErrors { get; set; } = new();

    public static EmailSendResult Succeeded(string messageId) => new()
    {
        Success = true,
        ProviderMessageId = messageId,
    };

    public static EmailSendResult Failed(string error, bool permanent = false) => new()
    {
        Success = false,
        IsPermanentFailure = permanent,
        ErrorMessage = error,
    };
}