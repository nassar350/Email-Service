namespace EmailManagement.Domain.Enums;

public enum EmailStatus
{
    Queued = 0,
    Processing = 1,
    Sent = 2,
    Retrying = 3,
    Delivered = 4,
    Bounced = 5,
    Blocked = 6,
    Opened = 7,
    Clicked = 8,
    Spam = 9,
    Unsubscribed = 10,
    Failed = 11,
}