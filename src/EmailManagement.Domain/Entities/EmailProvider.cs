using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.Entities;

public class EmailProvider
{
    public Guid ProviderId { get; set; }
    public ProviderType ProviderType { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string? Configuration { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Email> Emails { get; set; } = new List<Email>();
}