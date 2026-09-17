using EmailManagement.Domain.DomainServices;
using EmailManagement.Domain.Enums;
using FluentAssertions;

namespace EmailManagement.Tests.Unit.Domain;

public class RetryPolicyTests
{
    [Theory]
    [InlineData(0, 30)]
    [InlineData(1, 60)]
    [InlineData(2, 120)]
    public void Calculate_ReturnsExponentialBackoffDelay(int retryCount, int expectedSeconds)
    {
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var result = RetryPolicy.Calculate(retryCount, 5, now);

        result.Status.Should().Be(EmailStatus.Retrying);
        result.NextRetryAt.Should().Be(now.AddSeconds(expectedSeconds));
    }

    [Fact]
    public void Calculate_WhenRetryCountReachesMax_ReturnsFailed()
    {
        var now = DateTime.UtcNow;

        var result = RetryPolicy.Calculate(3, 3, now);

        result.Status.Should().Be(EmailStatus.Failed);
        result.NextRetryAt.Should().BeNull();
    }

    [Fact]
    public void Calculate_WhenRetryCountExceedsMax_ReturnsFailed()
    {
        var now = DateTime.UtcNow;

        var result = RetryPolicy.Calculate(10, 3, now);

        result.Status.Should().Be(EmailStatus.Failed);
        result.NextRetryAt.Should().BeNull();
    }
}