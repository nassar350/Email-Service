using EmailManagement.Application.DTOs.Requests;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Services;
using EmailManagement.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailManagement.Api.Controllers;

[ApiController]
[Route("api/v1/templates")]
[Authorize]
public class TemplateController : ControllerBase
{
    private readonly TemplateService _templateService;

    public TemplateController(TemplateService templateService)
    {
        _templateService = templateService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTemplateRequest request, CancellationToken ct)
    {
        var variables = request.Variables?.Select(v => new EmailManagement.Domain.Entities.TemplateVariable
        {
            VariableKey = v.VariableKey,
            DisplayName = v.DisplayName,
            DataType = v.DataType,
            IsRequired = v.IsRequired,
            DefaultValue = v.DefaultValue,
            Description = v.Description,
        }).ToList();

        var template = await _templateService.CreateAsync(
            request.TemplateKey, request.Name, request.Description,
            request.SubjectTemplate, request.HtmlBody, request.TextBody,
            variables, ct);

        return StatusCode(201, new ApiResponse<TemplateResponse>
        {
            Data = MapToResponse(template),
            Message = "Template created.",
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TemplateResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var templates = await _templateService.GetAllAsync(ct);
        return Ok(new ApiResponse<IReadOnlyList<TemplateResponse>>
        {
            Data = templates.Select(MapToResponse).ToList(),
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var template = await _templateService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<TemplateResponse> { Data = MapToResponse(template) });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTemplateRequest request, CancellationToken ct)
    {
        await _templateService.UpdateAsync(id, request, ct);
        var template = await _templateService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<TemplateResponse>
        {
            Data = MapToResponse(template),
            Message = "Template updated.",
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _templateService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(typeof(ApiResponse<TemplateVersionResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateVersion(Guid id, [FromBody] CreateVersionRequest request, CancellationToken ct)
    {
        var version = await _templateService.CreateVersionAsync(id, request.ChangeDescription, ct);
        return StatusCode(201, new ApiResponse<TemplateVersionResponse>
        {
            Data = new TemplateVersionResponse
            {
                VersionId = version.VersionId,
                VersionNumber = version.VersionNumber,
                SubjectTemplate = version.SubjectTemplate,
                HtmlBody = version.HtmlBody,
                TextBody = version.TextBody,
                ChangeDescription = version.ChangeDescription,
                IsActive = version.IsActive,
                CreatedAt = version.CreatedAt,
                CreatedBy = version.CreatedBy,
            },
            Message = "Version created.",
        });
    }

    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TemplateVersionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken ct)
    {
        var versions = await _templateService.GetVersionsAsync(id, ct);
        return Ok(new ApiResponse<IReadOnlyList<TemplateVersionResponse>>
        {
            Data = versions.Select(v => new TemplateVersionResponse
            {
                VersionId = v.VersionId,
                VersionNumber = v.VersionNumber,
                SubjectTemplate = v.SubjectTemplate,
                HtmlBody = v.HtmlBody,
                TextBody = v.TextBody,
                ChangeDescription = v.ChangeDescription,
                IsActive = v.IsActive,
                CreatedAt = v.CreatedAt,
                CreatedBy = v.CreatedBy,
            }).ToList(),
        });
    }

    [HttpGet("{id:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TemplateVersionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVersion(Guid id, Guid versionId, CancellationToken ct)
    {
        var version = await _templateService.GetVersionAsync(id, versionId, ct);
        return Ok(new ApiResponse<TemplateVersionResponse>
        {
            Data = new TemplateVersionResponse
            {
                VersionId = version.VersionId,
                VersionNumber = version.VersionNumber,
                SubjectTemplate = version.SubjectTemplate,
                HtmlBody = version.HtmlBody,
                TextBody = version.TextBody,
                ChangeDescription = version.ChangeDescription,
                IsActive = version.IsActive,
                CreatedAt = version.CreatedAt,
                CreatedBy = version.CreatedBy,
            },
        });
    }

    [HttpPost("{id:guid}/preview")]
    [ProducesResponseType(typeof(ApiResponse<TemplatePreviewResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Preview(Guid id, [FromBody] PreviewTemplateRequest request, CancellationToken ct)
    {
        var result = await _templateService.PreviewAsync(id, request.Variables, ct);
        return Ok(new ApiResponse<TemplatePreviewResponse> { Data = result });
    }

    private static TemplateResponse MapToResponse(Domain.Entities.EmailTemplate template) => new()
    {
        TemplateId = template.TemplateId,
        TemplateKey = template.TemplateKey,
        Name = template.Name,
        Description = template.Description,
        SubjectTemplate = template.SubjectTemplate,
        HtmlBody = template.HtmlBody,
        TextBody = template.TextBody,
        Status = template.Status,
        CurrentVersion = template.CurrentVersion,
        CreatedAt = template.CreatedAt,
        UpdatedAt = template.UpdatedAt,
        Variables = template.Variables.Select(v => new TemplateVariableResponse
        {
            VariableId = v.VariableId,
            VariableKey = v.VariableKey,
            DisplayName = v.DisplayName,
            DataType = v.DataType,
            IsRequired = v.IsRequired,
            DefaultValue = v.DefaultValue,
            Description = v.Description,
        }).ToList(),
    };
}