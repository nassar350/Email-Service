using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.Entities;

public class EmailTemplate
{
    public Guid TemplateId { get; set; }
    public string TemplateKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public TemplateStatus Status { get; set; }
    public int CurrentVersion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<TemplateVersion> Versions { get; set; } = new List<TemplateVersion>();
    public ICollection<TemplateVariable> Variables { get; set; } = new List<TemplateVariable>();
    public ICollection<Email> Emails { get; set; } = new List<Email>();
}