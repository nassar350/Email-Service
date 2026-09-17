using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Exceptions;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EmailManagement.Application.Services;

public class EmailQueryService
{
    private readonly IEmailRepository _emailRepo;
    private readonly IEmailEventRepository _eventRepo;
    private readonly IEmailLogRepository _logRepo;
    private readonly ILogger<EmailQueryService> _logger;

    public EmailQueryService(
        IEmailRepository emailRepo,
        IEmailEventRepository eventRepo,
        IEmailLogRepository logRepo,
        ILogger<EmailQueryService> logger)
    {
        _emailRepo = emailRepo;
        _eventRepo = eventRepo;
        _logRepo = logRepo;
        _logger = logger;
    }

    public async Task<EmailListResponse> QueryAsync(
        EmailStatus? status, string? templateKey, string? recipient,
        DateTime? dateFrom, DateTime? dateTo,
        int page, int pageSize, CancellationToken ct = default)
    {
        var (items, total) = await _emailRepo.QueryAsync(
            status, templateKey, recipient, dateFrom, dateTo, page, pageSize, ct);

        return new EmailListResponse
        {
            Items = items.Select(MapToResponse).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<EmailResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var email = await _emailRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Email), id);
        return MapToResponse(email);
    }

    public async Task<IReadOnlyList<EmailEventResponse>> GetEventsAsync(Guid emailId, CancellationToken ct = default)
    {
        var _ = await _emailRepo.GetByIdAsync(emailId, ct)
            ?? throw new NotFoundException(nameof(Email), emailId);

        var events = await _eventRepo.GetByEmailAsync(emailId, ct);
        return events.Select(e => new EmailEventResponse
        {
            EventId = e.EventId,
            EventType = e.EventType,
            EventData = e.EventData,
            OccurredAt = e.OccurredAt,
            RecipientEmail = e.Recipient?.Email,
        }).ToList();
    }

    public async Task<IReadOnlyList<EmailLogResponse>> GetLogsAsync(Guid emailId, CancellationToken ct = default)
    {
        var _ = await _emailRepo.GetByIdAsync(emailId, ct)
            ?? throw new NotFoundException(nameof(Email), emailId);

        var logs = await _logRepo.GetByEmailAsync(emailId, ct);
        return logs.Select(l => new EmailLogResponse
        {
            LogId = l.LogId,
            Level = l.Level,
            Message = l.Message,
            Details = l.Details,
            CreatedAt = l.CreatedAt,
        }).ToList();
    }

    public static EmailResponse MapToResponse(Domain.Entities.Email email) => new()
    {
        EmailId = email.EmailId,
        Subject = email.Subject,
        FromEmail = email.FromEmail,
        FromName = email.FromName,
        Status = email.Status,
        MessageId = email.MessageId,
        RetryCount = email.RetryCount,
        MaxRetries = email.MaxRetries,
        NextRetryAt = email.NextRetryAt,
        LastError = email.LastError,
        QueuedAt = email.QueuedAt,
        ProcessingAt = email.ProcessingAt,
        SentAt = email.SentAt,
        CreatedAt = email.CreatedAt,
        Recipients = email.Recipients.Select(r => new EmailRecipientResponse
        {
            RecipientId = r.RecipientId,
            Email = r.Email,
            RecipientType = r.RecipientType,
            Status = r.Status,
            DeliveredAt = r.DeliveredAt,
            OpenedAt = r.OpenedAt,
            ClickedAt = r.ClickedAt,
        }).ToList(),
    };
}