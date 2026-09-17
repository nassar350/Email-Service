using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.Entities;

public class EmailRecipient
{
    public Guid RecipientId { get; set; }
    public Guid EmailId { get; set; }
    public string Email { get; set; } = string.Empty;
    public RecipientType RecipientType { get; set; }
    public EmailStatus? Status { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClickedAt { get; set; }

    public Email EmailEntity { get; set; } = null!;
    public ICollection<EmailEvent> Events { get; set; } = new List<EmailEvent>();
}