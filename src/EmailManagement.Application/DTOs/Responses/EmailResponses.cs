using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.DTOs.Responses;

public record EmailResponse
{
    public Guid EmailId { get; init; }
    public string Subject { get; init; } = string.Empty;
    public string FromEmail { get; init; } = string.Empty;
    public string? FromName { get; init; }
    public EmailStatus Status { get; init; }
    public string? MessageId { get; init; }
    public int RetryCount { get; init; }
    public int MaxRetries { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public string? LastError { get; init; }
    public DateTime QueuedAt { get; init; }
    public DateTime? ProcessingAt { get; init; }
    public DateTime? SentAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public List<EmailRecipientResponse> Recipients { get; init; } = new();
}

public record EmailRecipientResponse
{
    public Guid RecipientId { get; init; }
    public string Email { get; init; } = string.Empty;
    public RecipientType RecipientType { get; init; }
    public EmailStatus? Status { get; init; }
    public DateTime? DeliveredAt { get; init; }
    public DateTime? OpenedAt { get; init; }
    public DateTime? ClickedAt { get; init; }
}

public record EmailEventResponse
{
    public Guid EventId { get; init; }
    public EmailEventType EventType { get; init; }
    public string? EventData { get; init; }
    public DateTime OccurredAt { get; init; }
    public string? RecipientEmail { get; init; }
}

public record EmailLogResponse
{
    public Guid LogId { get; init; }
    public LogLevel Level { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? Details { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record EmailListResponse
{
    public IReadOnlyList<EmailResponse> Items { get; init; } = Array.Empty<EmailResponse>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record EnqueuedEmailResponse
{
    public Guid EmailId { get; init; }
    public EmailStatus Status { get; init; }
    public DateTime QueuedAt { get; init; }
}