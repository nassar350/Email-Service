namespace EmailManagement.Infrastructure.Options;

public class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public int SendPermitLimit { get; set; } = 20;
    public int SendWindowSeconds { get; set; } = 60;
}