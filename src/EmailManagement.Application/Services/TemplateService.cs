using EmailManagement.Application.DTOs.Requests;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Exceptions;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EmailManagement.Application.Services;

public class TemplateService
{
    private readonly ITemplateRepository _templateRepo;
    private readonly ITemplateVersionRepository _versionRepo;
    private readonly ITemplateRenderer _renderer;
    private readonly ILogger<TemplateService> _logger;

    public TemplateService(
        ITemplateRepository templateRepo,
        ITemplateVersionRepository versionRepo,
        ITemplateRenderer renderer,
        ILogger<TemplateService> logger)
    {
        _templateRepo = templateRepo;
        _versionRepo = versionRepo;
        _renderer = renderer;
        _logger = logger;
    }

    public async Task<EmailTemplate> CreateAsync(
        string templateKey, string name, string? description,
        string subjectTemplate, string htmlBody, string? textBody,
        List<TemplateVariable>? variables,
        CancellationToken ct = default)
    {
        if (await _templateRepo.ExistsByKeyAsync(templateKey, ct: ct))
            throw new ConflictException($"A template with key '{templateKey}' already exists.");

        var now = DateTime.UtcNow;
        var template = new EmailTemplate
        {
            TemplateId = Guid.NewGuid(),
            TemplateKey = templateKey,
            Name = name,
            Description = description,
            SubjectTemplate = subjectTemplate,
            HtmlBody = htmlBody,
            TextBody = textBody,
            Status = TemplateStatus.Draft,
            CurrentVersion = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var version = new TemplateVersion
        {
            VersionId = Guid.NewGuid(),
            TemplateId = template.TemplateId,
            VersionNumber = 1,
            SubjectTemplate = subjectTemplate,
            HtmlBody = htmlBody,
            TextBody = textBody,
            ChangeDescription = "Initial version",
            IsActive = true,
            CreatedAt = now,
        };

        template.Versions.Add(version);

        if (variables?.Count > 0)
        {
            foreach (var v in variables)
            {
                template.Variables.Add(new TemplateVariable
                {
                    VariableId = Guid.NewGuid(),
                    TemplateId = template.TemplateId,
                    VariableKey = v.VariableKey,
                    DisplayName = v.DisplayName,
                    DataType = v.DataType,
                    IsRequired = v.IsRequired,
                    DefaultValue = v.DefaultValue,
                    Description = v.Description,
                    CreatedAt = now,
                });
            }
        }

        await _templateRepo.AddAsync(template, ct);

        _logger.LogInformation("Template created: {Key} (id={Id})", templateKey, template.TemplateId);
        return template;
    }

    public async Task<EmailTemplate> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _templateRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(EmailTemplate), id);
    }

    public async Task<IReadOnlyList<EmailTemplate>> GetAllAsync(CancellationToken ct = default)
    {
        return await _templateRepo.GetAllAsync(ct);
    }

    public async Task UpdateAsync(Guid id, UpdateTemplateRequest request, CancellationToken ct = default)
    {
        var template = await GetByIdAsync(id, ct);

        if (request.Name is not null) template.Name = request.Name;
        if (request.Description is not null) template.Description = request.Description;
        if (request.Status is not null)
        {
            if (!Enum.TryParse<TemplateStatus>(request.Status, true, out var status))
                throw new ValidationException(new List<ValidationError>
                {
                    new("status", $"Invalid status value: {request.Status}.")
                });
            template.Status = status;
        }

        template.UpdatedAt = DateTime.UtcNow;
        await _templateRepo.UpdateAsync(template, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await GetByIdAsync(id, ct);

        if (await _templateRepo.HasEmailsAsync(id, ct))
            throw new ConflictException("Cannot delete a template that has associated emails. Archive it instead.");

        await _templateRepo.DeleteAsync(template, ct);
        _logger.LogInformation("Template deleted: {Key}", template.TemplateKey);
    }

    public async Task<TemplateVersion> CreateVersionAsync(
        Guid templateId, string? changeDescription, CancellationToken ct = default)
    {
        var template = await GetByIdAsync(templateId, ct);

        var activeVersion = await _versionRepo.GetActiveAsync(templateId, ct);
        if (activeVersion is not null)
        {
            activeVersion.IsActive = false;
            await _versionRepo.UpdateAsync(activeVersion, ct);
        }

        var versionNumber = await _versionRepo.GetMaxVersionNumberAsync(templateId, ct) + 1;

        var version = new TemplateVersion
        {
            VersionId = Guid.NewGuid(),
            TemplateId = templateId,
            VersionNumber = versionNumber,
            SubjectTemplate = template.SubjectTemplate,
            HtmlBody = template.HtmlBody,
            TextBody = template.TextBody,
            ChangeDescription = changeDescription ?? $"Version {versionNumber}",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        template.CurrentVersion = versionNumber;
        template.UpdatedAt = DateTime.UtcNow;

        await _versionRepo.AddAsync(version, ct);
        await _templateRepo.UpdateAsync(template, ct);

        _logger.LogInformation("Template version {Version} created for {Key}", versionNumber, template.TemplateKey);
        return version;
    }

    public async Task<IReadOnlyList<TemplateVersion>> GetVersionsAsync(Guid templateId, CancellationToken ct = default)
    {
        var _ = await GetByIdAsync(templateId, ct);
        return await _versionRepo.GetByTemplateAsync(templateId, ct);
    }

    public async Task<TemplateVersion> GetVersionAsync(Guid templateId, Guid versionId, CancellationToken ct = default)
    {
        var _ = await GetByIdAsync(templateId, ct);
        var version = await _versionRepo.GetByIdAsync(versionId, ct)
            ?? throw new NotFoundException(nameof(TemplateVersion), versionId);
        return version;
    }

    public async Task<TemplatePreviewResponse> PreviewAsync(
        Guid templateId, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        var template = await GetByIdAsync(templateId, ct);
        var version = await _versionRepo.GetActiveAsync(templateId, ct)
            ?? await _versionRepo.GetByTemplateAsync(templateId, ct)
            .ContinueWith(t => t.Result.LastOrDefault(), ct);

        if (version is null)
            throw new ConflictException("No versions available for this template.");

        return new TemplatePreviewResponse
        {
            Subject = _renderer.Render(version.SubjectTemplate, variables),
            HtmlBody = _renderer.Render(version.HtmlBody, variables, htmlEncode: true),
            TextBody = version.TextBody is not null ? _renderer.Render(version.TextBody, variables) : null,
        };
    }

    public static void ValidateRequiredVariables(EmailTemplate template, Dictionary<string, string> providedVariables)
    {
        var missing = template.Variables
            .Where(v => v.IsRequired)
            .Where(v => !providedVariables.ContainsKey(v.VariableKey) ||
                        string.IsNullOrWhiteSpace(providedVariables[v.VariableKey]))
            .Select(v => new ValidationError($"variables.{v.VariableKey}", $"The variable is required."))
            .ToList();

        if (missing.Count > 0)
            throw new ValidationException(missing);
    }
}