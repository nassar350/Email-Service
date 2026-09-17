namespace EmailManagement.Domain.Entities;

public class TemplateVersion
{
    public Guid VersionId { get; set; }
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; }
    public string SubjectTemplate { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public string? TextBody { get; set; }
    public string? ChangeDescription { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public EmailTemplate Template { get; set; } = null!;
    public ICollection<Email> Emails { get; set; } = new List<Email>();
}