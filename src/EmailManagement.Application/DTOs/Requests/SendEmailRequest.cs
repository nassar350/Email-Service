namespace EmailManagement.Application.DTOs.Requests;

public record SendEmailRequest
{
    public string TemplateKey { get; init; } = string.Empty;
    public List<string> To { get; init; } = new();
    public List<string> Cc { get; init; } = new();
    public List<string> Bcc { get; init; } = new();
    public Dictionary<string, string> Variables { get; init; } = new();
}