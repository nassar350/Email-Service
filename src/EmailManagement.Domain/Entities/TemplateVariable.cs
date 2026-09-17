namespace EmailManagement.Domain.Entities;

public class TemplateVariable
{
    public Guid VariableId { get; set; }
    public Guid TemplateId { get; set; }
    public string VariableKey { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string DataType { get; set; } = "string";
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public EmailTemplate Template { get; set; } = null!;
}