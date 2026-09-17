namespace EmailManagement.Application.DTOs.Requests;

public record CreateTemplateRequest
{
    public string TemplateKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string SubjectTemplate { get; init; } = string.Empty;
    public string HtmlBody { get; init; } = string.Empty;
    public string? TextBody { get; init; }
    public List<CreateTemplateVariableRequest> Variables { get; init; } = new();
}

public record UpdateTemplateRequest
{
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? SubjectTemplate { get; init; }
    public string? HtmlBody { get; init; }
    public string? TextBody { get; init; }
    public string? Status { get; init; }
}

public record CreateTemplateVariableRequest
{
    public string VariableKey { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public string DataType { get; init; } = "string";
    public bool IsRequired { get; init; }
    public string? DefaultValue { get; init; }
    public string? Description { get; init; }
}

public record CreateVersionRequest
{
    public string? ChangeDescription { get; init; }
}

public record PreviewTemplateRequest
{
    public Dictionary<string, string> Variables { get; init; } = new();
}