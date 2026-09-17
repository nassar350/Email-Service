namespace EmailManagement.Domain.Enums;

public enum EmailEventType
{
    Queued = 0,
    Processing = 1,
    Sent = 2,
    Delivered = 3,
    Bounced = 4,
    Blocked = 5,
    Opened = 6,
    Clicked = 7,
    Spam = 8,
    Unsubscribed = 9,
    Failed = 10,
}