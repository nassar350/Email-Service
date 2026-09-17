using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.Entities;

public class EmailLog
{
    public Guid LogId { get; set; }
    public Guid? EmailId { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }

    public Email? EmailEntity { get; set; }
}