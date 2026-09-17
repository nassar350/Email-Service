using System.Text.Json;
using System.Text.Json.Serialization;
using EmailManagement.Application.Services;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Infrastructure.Providers;

public static class MailjetWebhookMapper
{
    private static readonly Dictionary<string, EmailEventType> EventTypeMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["sent"] = EmailEventType.Sent,
        ["open"] = EmailEventType.Opened,
        ["click"] = EmailEventType.Clicked,
        ["bounce"] = EmailEventType.Bounced,
        ["blocked"] = EmailEventType.Blocked,
        ["spam"] = EmailEventType.Spam,
        ["unsub"] = EmailEventType.Unsubscribed,
        ["delivered"] = EmailEventType.Delivered,
    };

    public static IReadOnlyList<WebhookEvent> MapEvents(string requestBody)
    {
        if (string.IsNullOrWhiteSpace(requestBody))
            return Array.Empty<WebhookEvent>();

        List<MailjetWebhookEvent>? mailjetEvents;
        try
        {
            mailjetEvents = JsonSerializer.Deserialize<List<MailjetWebhookEvent>>(requestBody);
        }
        catch (JsonException)
        {
            return Array.Empty<WebhookEvent>();
        }

        if (mailjetEvents is null) return Array.Empty<WebhookEvent>();

        var result = new List<WebhookEvent>();

        foreach (var mjEvent in mailjetEvents)
        {
            if (string.IsNullOrEmpty(mjEvent.Event)) continue;

            if (!EventTypeMap.TryGetValue(mjEvent.Event, out var eventType))
                continue;

            var providerEventId = mjEvent.EventGuid;
            if (string.IsNullOrEmpty(providerEventId))
            {
                providerEventId = $"{mjEvent.Event}|{mjEvent.MessageId}|{mjEvent.Email}|{mjEvent.Time}";
            }

            var occurredAt = mjEvent.Time > 0
                ? DateTimeOffset.FromUnixTimeSeconds(mjEvent.Time).UtcDateTime
                : DateTime.UtcNow;

            result.Add(new WebhookEvent
            {
                ProviderEventId = providerEventId,
                ProviderMessageId = mjEvent.MessageId?.ToString(),
                RecipientEmail = mjEvent.Email,
                EventType = eventType,
                RawEventData = requestBody.Length > 4000 ? requestBody[..4000] : requestBody,
                OccurredAt = occurredAt,
            });
        }

        return result;
    }
}

public class MailjetWebhookEvent
{
    [JsonPropertyName("event")]
    public string? Event { get; set; }

    [JsonPropertyName("time")]
    public long Time { get; set; }

    [JsonPropertyName("MessageID")]
    public long? MessageId { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("event_guid")]
    public string? EventGuid { get; set; }

    [JsonPropertyName("Message-Id")]
    public string? MessageIdAlt { get; set; }

    [JsonPropertyName("customcampaignid")]
    public string? CustomCampaignId { get; set; }
}