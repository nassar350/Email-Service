using System.Text.Json;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.DomainServices;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using Microsoft.Extensions.Logging;
using DomainLogLevel = EmailManagement.Domain.Enums.LogLevel;

namespace EmailManagement.Application.Services;

public class EmailProcessingService
{
    private readonly IEmailRepository _emailRepo;
    private readonly IEmailRecipientRepository _recipientRepo;
    private readonly IEmailEventRepository _eventRepo;
    private readonly IEmailLogRepository _logRepo;
    private readonly ITemplateVersionRepository _versionRepo;
    private readonly IEmailProviderFactory _providerFactory;
    private readonly ITemplateRenderer _renderer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailProcessingService> _logger;

    public EmailProcessingService(
        IEmailRepository emailRepo,
        IEmailRecipientRepository recipientRepo,
        IEmailEventRepository eventRepo,
        IEmailLogRepository logRepo,
        ITemplateVersionRepository versionRepo,
        IEmailProviderFactory providerFactory,
        ITemplateRenderer renderer,
        IUnitOfWork unitOfWork,
        ILogger<EmailProcessingService> logger)
    {
        _emailRepo = emailRepo;
        _recipientRepo = recipientRepo;
        _eventRepo = eventRepo;
        _logRepo = logRepo;
        _versionRepo = versionRepo;
        _providerFactory = providerFactory;
        _renderer = renderer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task ProcessBatchAsync(int batchSize, CancellationToken ct = default)
    {
        var eligible = await _emailRepo.GetEligibleForProcessingAsync(batchSize, ct);

        foreach (var email in eligible)
        {
            if (!await _emailRepo.ClaimForProcessingAsync(email.EmailId, ct))
                continue;

            await ProcessEmailAsync(email, ct);
        }
    }

    private async Task ProcessEmailAsync(Email email, CancellationToken ct)
    {
        email.Status = EmailStatus.Processing;
        email.ProcessingAt = DateTime.UtcNow;
        await _emailRepo.UpdateAsync(email, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        try
        {
            await SendEmailAsync(email, ct);
        }
        catch (Exception ex)
        {
            await HandleFailureAsync(email, ex, ct);
        }
    }

    private async Task SendEmailAsync(Email email, CancellationToken ct)
    {
        var version = await _versionRepo.GetByIdAsync(email.VersionId, ct);
        var providerEntity = email.Provider;

        var provider = _providerFactory.GetProvider(providerEntity.ProviderType);
        var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(email.TemplateData ?? "{}") ?? new();

        var message = new EmailMessage
        {
            EmailId = email.EmailId,
            Subject = email.Subject,
            FromEmail = email.FromEmail,
            FromName = email.FromName,
            ReplyTo = email.ReplyTo,
            Recipients = email.Recipients.Select(r => new EmailRecipientAddress
            {
                Email = r.Email,
                RecipientType = r.RecipientType,
            }).ToList(),
            HtmlBody = version is not null
                ? _renderer.Render(version.HtmlBody, variables, htmlEncode: true)
                : null,
            TextBody = version?.TextBody is not null
                ? _renderer.Render(version.TextBody, variables)
                : null,
        };

        _logger.LogInformation("Attempting to send email {EmailId} via {ProviderType}",
            email.EmailId, providerEntity.ProviderType);

        var result = await provider.SendAsync(message, ct);

        if (result.Success)
        {
            email.Status = EmailStatus.Sent;
            email.MessageId = result.ProviderMessageId;
            email.SentAt = DateTime.UtcNow;

            await _eventRepo.AddAsync(new EmailEvent
            {
                EventId = Guid.NewGuid(),
                EmailId = email.EmailId,
                EventType = EmailEventType.Sent,
                OccurredAt = DateTime.UtcNow,
            }, ct);

            await _logRepo.AddAsync(new EmailLog
            {
                LogId = Guid.NewGuid(),
                EmailId = email.EmailId,
                Level = DomainLogLevel.Information,
                Message = $"Email accepted by {providerEntity.ProviderType}. MessageId: {result.ProviderMessageId}",
                CreatedAt = DateTime.UtcNow,
            }, ct);

            _logger.LogInformation("Email {EmailId} accepted by provider. MessageId: {MessageId}",
                email.EmailId, result.ProviderMessageId);
        }
        else
        {
            throw new EmailSendException(result.ErrorMessage ?? "Unknown error", result.IsPermanentFailure);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task HandleFailureAsync(Email email, Exception ex, CancellationToken ct)
    {
        var isPermanent = ex is EmailSendException sendEx && sendEx.IsPermanent;
        var sanitizedError = SanitizeError(ex.Message);

        email.RetryCount++;
        email.LastError = sanitizedError;

        if (isPermanent || email.RetryCount >= email.MaxRetries)
        {
            email.Status = EmailStatus.Failed;
            email.LastError = sanitizedError;

            await _eventRepo.AddAsync(new EmailEvent
            {
                EventId = Guid.NewGuid(),
                EmailId = email.EmailId,
                EventType = EmailEventType.Failed,
                EventData = JsonSerializer.Serialize(new { error = sanitizedError, permanent = isPermanent }),
                OccurredAt = DateTime.UtcNow,
            }, ct);

            await _logRepo.AddAsync(new EmailLog
            {
                LogId = Guid.NewGuid(),
                EmailId = email.EmailId,
                Level = DomainLogLevel.Error,
                Message = $"Email failed: {sanitizedError}",
                CreatedAt = DateTime.UtcNow,
            }, ct);

            _logger.LogError("Email {EmailId} failed permanently: {Error}", email.EmailId, sanitizedError);
        }
        else
        {
            var retry = RetryPolicy.Calculate(email.RetryCount - 1, email.MaxRetries, DateTime.UtcNow);
            email.Status = retry.Status;
            email.NextRetryAt = retry.NextRetryAt;

            await _eventRepo.AddAsync(new EmailEvent
            {
                EventId = Guid.NewGuid(),
                EmailId = email.EmailId,
                EventType = EmailEventType.Failed,
                EventData = JsonSerializer.Serialize(new { error = sanitizedError, scheduledRetry = retry.NextRetryAt }),
                OccurredAt = DateTime.UtcNow,
            }, ct);

            await _logRepo.AddAsync(new EmailLog
            {
                LogId = Guid.NewGuid(),
                EmailId = email.EmailId,
                Level = DomainLogLevel.Warning,
                Message = $"Email failed (attempt {email.RetryCount}/{email.MaxRetries}). Scheduled retry at {retry.NextRetryAt:O}.",
                CreatedAt = DateTime.UtcNow,
            }, ct);

            _logger.LogWarning("Email {EmailId} failed (attempt {Count}/{Max}), retry at {RetryAt}",
                email.EmailId, email.RetryCount, email.MaxRetries, retry.NextRetryAt);
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static string SanitizeError(string error)
    {
        var sanitized = error.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        return sanitized.Length > 500 ? sanitized[..500] : sanitized;
    }
}

public class EmailSendException : Exception
{
    public bool IsPermanent { get; }

    public EmailSendException(string message, bool isPermanent)
        : base(message)
    {
        IsPermanent = isPermanent;
    }
}