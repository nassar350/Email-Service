namespace EmailManagement.Infrastructure.Options;

public class WorkerOptions
{
    public const string SectionName = "Worker";
    public int PollingIntervalSeconds { get; set; } = 10;
    public int BatchSize { get; set; } = 5;
}