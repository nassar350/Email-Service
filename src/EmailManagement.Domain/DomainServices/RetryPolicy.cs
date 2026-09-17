namespace EmailManagement.Domain.DomainServices;

public static class RetryPolicy
{
    public const int DefaultMaxRetries = 3;
    public const int BaseDelaySeconds = 30;

    public static RetryResult Calculate(int retryCount, int maxRetries, DateTime utcNow)
    {
        if (retryCount >= maxRetries)
            return new RetryResult(Enums.EmailStatus.Failed, null);

        var delay = TimeSpan.FromSeconds(BaseDelaySeconds * Math.Pow(2, retryCount));
        return new RetryResult(Enums.EmailStatus.Retrying, utcNow + delay);
    }
}

public record RetryResult(Enums.EmailStatus Status, DateTime? NextRetryAt);