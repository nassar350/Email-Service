using EmailManagement.Domain.DomainServices;
using EmailManagement.Domain.Enums;
using FluentAssertions;

namespace EmailManagement.Tests.Unit.Domain;

public class EmailStatusTransitionValidatorTests
{
    [Theory]
    [InlineData(EmailStatus.Queued, EmailStatus.Processing, true)]
    [InlineData(EmailStatus.Processing, EmailStatus.Sent, true)]
    [InlineData(EmailStatus.Processing, EmailStatus.Retrying, true)]
    [InlineData(EmailStatus.Processing, EmailStatus.Failed, true)]
    [InlineData(EmailStatus.Retrying, EmailStatus.Processing, true)]
    [InlineData(EmailStatus.Retrying, EmailStatus.Failed, true)]
    [InlineData(EmailStatus.Queued, EmailStatus.Sent, false)]
    [InlineData(EmailStatus.Queued, EmailStatus.Failed, false)]
    [InlineData(EmailStatus.Sent, EmailStatus.Processing, false)]
    [InlineData(EmailStatus.Processing, EmailStatus.Opened, false)]
    public void CanWorkerTransition_RespectsAllowedTransitions(EmailStatus from, EmailStatus to, bool expected)
    {
        EmailStatusTransitionValidator.CanWorkerTransition(from, to).Should().Be(expected);
    }

    [Theory]
    [InlineData(EmailStatus.Sent, EmailStatus.Opened, true)]
    [InlineData(EmailStatus.Sent, EmailStatus.Delivered, true)]
    [InlineData(EmailStatus.Opened, EmailStatus.Sent, false)]
    [InlineData(EmailStatus.Delivered, EmailStatus.Opened, false)]
    public void CanWebhookTransition_RequiresMonotonicPrecedence(EmailStatus current, EmailStatus incoming, bool expected)
    {
        EmailStatusTransitionValidator.CanWebhookTransition(current, incoming).Should().Be(expected);
    }

    [Fact]
    public void CanWebhookTransition_WhenCurrentIsFailed_AlwaysFalse()
    {
        EmailStatusTransitionValidator.CanWebhookTransition(EmailStatus.Failed, EmailStatus.Opened).Should().BeFalse();
    }

    [Theory]
    [InlineData(EmailStatus.Opened, EmailStatus.Failed)]
    [InlineData(EmailStatus.Sent, EmailStatus.Retrying)]
    public void CanWebhookTransition_WhenIncomingIsFailedOrRetrying_AlwaysFalse(EmailStatus current, EmailStatus incoming)
    {
        EmailStatusTransitionValidator.CanWebhookTransition(current, incoming).Should().BeFalse();
    }

    [Fact]
    public void ResolveWebhookStatus_ReturnsIncomingWhenAllowed()
    {
        EmailStatusTransitionValidator.ResolveWebhookStatus(EmailStatus.Sent, EmailStatus.Delivered)
            .Should().Be(EmailStatus.Delivered);
    }

    [Fact]
    public void ResolveWebhookStatus_ReturnsCurrentWhenNotAllowed()
    {
        EmailStatusTransitionValidator.ResolveWebhookStatus(EmailStatus.Delivered, EmailStatus.Opened)
            .Should().Be(EmailStatus.Delivered);
    }
}