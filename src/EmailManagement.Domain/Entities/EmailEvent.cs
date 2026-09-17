using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.Entities;

public class EmailEvent
{
    public Guid EventId { get; set; }
    public Guid EmailId { get; set; }
    public Guid? RecipientId { get; set; }
    public EmailEventType EventType { get; set; }
    public string? EventData { get; set; }
    public string? ProviderEventId { get; set; }
    public DateTime OccurredAt { get; set; }

    public Email EmailEntity { get; set; } = null!;
    public EmailRecipient? Recipient { get; set; }
}