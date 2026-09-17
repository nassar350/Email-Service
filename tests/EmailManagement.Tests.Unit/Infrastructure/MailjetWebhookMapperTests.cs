using EmailManagement.Domain.Enums;
using EmailManagement.Infrastructure.Providers;
using FluentAssertions;

namespace EmailManagement.Tests.Unit.Infrastructure;

public class MailjetWebhookMapperTests
{
    [Fact]
    public void MapEvents_MapsAllKnownEventTypes()
    {
        const string body = """
        [
          { "event": "sent", "time": 1700000000, "MessageID": 1001, "email": "a@example.com", "event_guid": "g1" },
          { "event": "open", "time": 1700000001, "MessageID": 1002, "email": "b@example.com", "event_guid": "g2" },
          { "event": "click", "time": 1700000002, "MessageID": 1003, "email": "c@example.com", "event_guid": "g3" },
          { "event": "bounce", "time": 1700000003, "MessageID": 1004, "email": "d@example.com", "event_guid": "g4" },
          { "event": "blocked", "time": 1700000004, "MessageID": 1005, "email": "e@example.com", "event_guid": "g5" },
          { "event": "spam", "time": 1700000005, "MessageID": 1006, "email": "f@example.com", "event_guid": "g6" },
          { "event": "unsub", "time": 1700000006, "MessageID": 1007, "email": "g@example.com", "event_guid": "g7" },
          { "event": "delivered", "time": 1700000007, "MessageID": 1008, "email": "h@example.com", "event_guid": "g8" }
        ]
        """;

        var events = MailjetWebhookMapper.MapEvents(body);

        events.Should().HaveCount(8);
        events.Select(e => e.EventType).Should().Equal(
            EmailEventType.Sent,
            EmailEventType.Opened,
            EmailEventType.Clicked,
            EmailEventType.Bounced,
            EmailEventType.Blocked,
            EmailEventType.Spam,
            EmailEventType.Unsubscribed,
            EmailEventType.Delivered);
    }

    [Fact]
    public void MapEvents_SkipsUnknownAndEmptyEvents()
    {
        const string body = """
        [
          { "event": "unknown_event", "time": 1, "email": "a@example.com" },
          { "time": 2, "email": "b@example.com" }
        ]
        """;

        var events = MailjetWebhookMapper.MapEvents(body);

        events.Should().BeEmpty();
    }

    [Fact]
    public void MapEvents_GeneratesFallbackIdWhenEventGuidMissing()
    {
        const string body = """
        [ { "event": "open", "time": 1700000000, "MessageID": 42, "email": "a@example.com" } ]
        """;

        var events = MailjetWebhookMapper.MapEvents(body);

        events.Should().ContainSingle();
        events[0].ProviderEventId.Should().Be("open|42|a@example.com|1700000000");
    }

    [Fact]
    public void MapEvents_UsesEventGuidWhenPresent()
    {
        const string body = """
        [ { "event": "open", "event_guid": "abc-123", "email": "a@example.com" } ]
        """;

        var events = MailjetWebhookMapper.MapEvents(body);

        events.Single().ProviderEventId.Should().Be("abc-123");
    }

    [Fact]
    public void MapEvents_ConvertsUnixSecondsToUtc()
    {
        const string body = """
        [ { "event": "sent", "time": 1700000000, "email": "a@example.com", "event_guid": "g1" } ]
        """;

        var events = MailjetWebhookMapper.MapEvents(body);

        events.Single().OccurredAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1700000000).UtcDateTime);
    }

    [Fact]
    public void MapEvents_EmptyBody_ReturnsEmpty()
    {
        MailjetWebhookMapper.MapEvents("[]").Should().BeEmpty();
    }

    [Fact]
    public void MapEvents_InvalidJson_ReturnsEmpty()
    {
        MailjetWebhookMapper.MapEvents("").Should().BeEmpty();
    }
}