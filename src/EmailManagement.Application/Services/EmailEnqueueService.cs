using System.Text.Json;
using EmailManagement.Application.Exceptions;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.DomainServices;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EmailManagement.Application.Services;

public class EmailEnqueueService : IEmailEnqueueService
{
    private readonly ITemplateRepository _templateRepo;
    private readonly ITemplateVersionRepository _versionRepo;
    private readonly IProviderRepository _providerRepo;
    private readonly IEmailRepository _emailRepo;
    private readonly IEmailRecipientRepository _recipientRepo;
    private readonly IEmailEventRepository _eventRepo;
    private readonly IEmailLogRepository _logRepo;
    private readonly ITemplateRenderer _renderer;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailEnqueueService> _logger;

    public EmailEnqueueService(
        ITemplateRepository templateRepo,
        ITemplateVersionRepository versionRepo,
        IProviderRepository providerRepo,
        IEmailRepository emailRepo,
        IEmailRecipientRepository recipientRepo,
        IEmailEventRepository eventRepo,
        IEmailLogRepository logRepo,
        ITemplateRenderer renderer,
        IUnitOfWork unitOfWork,
        ILogger<EmailEnqueueService> logger)
    {
        _templateRepo = templateRepo;
        _versionRepo = versionRepo;
        _providerRepo = providerRepo;
        _emailRepo = emailRepo;
        _recipientRepo = recipientRepo;
        _eventRepo = eventRepo;
        _logRepo = logRepo;
        _renderer = renderer;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Email> EnqueueAsync(
        string templateKey,
        IReadOnlyList<string> to,
        IReadOnlyList<string> cc,
        IReadOnlyList<string> bcc,
        Dictionary<string, string> variables,
        CancellationToken ct = default)
    {
        var template = await _templateRepo.GetByKeyAsync(templateKey, ct)
            ?? throw new NotFoundException(nameof(EmailTemplate), templateKey);

        if (template.Status != TemplateStatus.Active)
            throw new ConflictException($"Template '{templateKey}' is not active (current status: {template.Status}).");

        var version = await _versionRepo.GetActiveAsync(template.TemplateId, ct)
            ?? throw new ConflictException($"No active version found for template '{templateKey}'.");

        TemplateService.ValidateRequiredVariables(template, variables);

        var provider = await _providerRepo.GetDefaultActiveAsync(ct: ct)
            ?? throw new ConflictException("No active email provider is configured.");

        var renderedSubject = _renderer.Render(version.SubjectTemplate, variables);
        var renderedHtml = _renderer.Render(version.HtmlBody, variables, htmlEncode: true);
        var renderedText = version.TextBody is not null ? _renderer.Render(version.TextBody, variables) : null;

        ValidateEmailAddresses(to, cc, bcc);
        ValidateSubject(renderedSubject);

        var email = new Email
        {
            EmailId = Guid.NewGuid(),
            TemplateId = template.TemplateId,
            VersionId = version.VersionId,
            ProviderId = provider.ProviderId,
            Status = EmailStatus.Queued,
            Subject = renderedSubject,
            FromEmail = provider.Configuration is not null
                ? ExtractFromConfig(provider.Configuration, "fromEmail") : "",
            FromName = provider.Configuration is not null
                ? ExtractFromConfig(provider.Configuration, "fromName") : null,
            TemplateData = JsonSerializer.Serialize(variables),
            RetryCount = 0,
            MaxRetries = RetryPolicy.DefaultMaxRetries,
            QueuedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
        };

        await _emailRepo.AddAsync(email, ct);

        var allRecipients = BuildRecipients(to, RecipientType.To, cc, RecipientType.Cc, bcc, RecipientType.Bcc);
        var emailRecipients = allRecipients.Select(r => new EmailRecipient
        {
            RecipientId = Guid.NewGuid(),
            EmailId = email.EmailId,
            Email = r.Email,
            RecipientType = r.Type,
        }).ToList();

        await _recipientRepo.AddRangeAsync(emailRecipients, ct);

        await _eventRepo.AddAsync(new EmailEvent
        {
            EventId = Guid.NewGuid(),
            EmailId = email.EmailId,
            EventType = EmailEventType.Queued,
            OccurredAt = DateTime.UtcNow,
        }, ct);

        await _logRepo.AddAsync(new EmailLog
        {
            LogId = Guid.NewGuid(),
            EmailId = email.EmailId,
            Level = Domain.Enums.LogLevel.Information,
            Message = "Email queued",
            CreatedAt = DateTime.UtcNow,
        }, ct);

        await _unitOfWork.SaveChangesAsync(ct);

        _logger.LogInformation("Email queued: {EmailId}, template: {Key}", email.EmailId, templateKey);
        return email;
    }

    private static void ValidateEmailAddresses(IReadOnlyList<string> to, IReadOnlyList<string> cc, IReadOnlyList<string> bcc)
    {
        var errors = new List<ValidationError>();

        void ValidateList(IReadOnlyList<string> emails, string field)
        {
            if (emails.Count == 0 && field == "to")
                errors.Add(new ValidationError(field, "At least one 'to' recipient is required."));

            foreach (var e in emails)
            {
                if (e.Contains('\r') || e.Contains('\n'))
                    errors.Add(new ValidationError(field, "Email address contains invalid characters."));
                else if (!IsValidEmail(e))
                    errors.Add(new ValidationError(field, $"'{e}' is not a valid email address."));
            }
        }

        ValidateList(to, "to");
        ValidateList(cc, "cc");
        ValidateList(bcc, "bcc");

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch { return false; }
    }

    private static void ValidateSubject(string subject)
    {
        if (subject.Contains('\r') || subject.Contains('\n'))
            throw new ValidationException(new List<ValidationError>
            {
                new("subject", "Subject contains invalid characters (CRLF).")
            });
    }

    private static List<(string Email, RecipientType Type)> BuildRecipients(
        IReadOnlyList<string> to, RecipientType toType,
        IReadOnlyList<string> cc, RecipientType ccType,
        IReadOnlyList<string> bcc, RecipientType bccType)
    {
        var result = new List<(string, RecipientType)>();
        result.AddRange(to.Select(e => (e, toType)));
        result.AddRange(cc.Select(e => (e, ccType)));
        result.AddRange(bcc.Select(e => (e, bccType)));
        return result;
    }

    private static string ExtractFromConfig(string configuration, string key)
    {
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(configuration);
            if (dict is not null && dict.TryGetValue(key, out var value))
                return value ?? string.Empty;
        }
        catch { }
        return string.Empty;
    }
}