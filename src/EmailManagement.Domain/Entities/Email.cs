using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.Entities;

public class Email
{
    public Guid EmailId { get; set; }
    public Guid TemplateId { get; set; }
    public Guid VersionId { get; set; }
    public Guid ProviderId { get; set; }
    public string? MessageId { get; set; }
    public EmailStatus Status { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }
    public string? TemplateData { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? LastError { get; set; }
    public DateTime QueuedAt { get; set; }
    public DateTime? ProcessingAt { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public EmailTemplate Template { get; set; } = null!;
    public TemplateVersion Version { get; set; } = null!;
    public EmailProvider Provider { get; set; } = null!;
    public ICollection<EmailRecipient> Recipients { get; set; } = new List<EmailRecipient>();
    public ICollection<EmailAttachment> Attachments { get; set; } = new List<EmailAttachment>();
    public ICollection<EmailEvent> Events { get; set; } = new List<EmailEvent>();
    public ICollection<EmailLog> Logs { get; set; } = new List<EmailLog>();
}