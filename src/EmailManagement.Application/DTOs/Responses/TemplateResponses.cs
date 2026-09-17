using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.DTOs.Responses;

public record TemplateResponse
{
    public Guid TemplateId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SubjectTemplate { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public TemplateStatus Status { get; init; }
    public int CurrentVersion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public List<TemplateVariableResponse> Variables { get; init; } = new();
}

public record TemplateVersionResponse
{
    public Guid VersionId { get; init; }
    public int VersionNumber { get; init; }
    public string SubjectTemplate { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public string? ChangeDescription { get; init; }
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public string? CreatedBy { get; init; }
}

public record TemplateVariableResponse
{
    public Guid VariableId { get; init; }
    public string VariableKey { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string DataType { get; init; } = "string";
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public string? Description { get; init; }
}

public record TemplatePreviewResponse
{
    public string Subject { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? TextBody { get; init; }
}