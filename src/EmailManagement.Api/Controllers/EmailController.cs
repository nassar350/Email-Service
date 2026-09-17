using EmailManagement.Application.DTOs.Requests;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Ports;
using EmailManagement.Application.Services;
using EmailManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace EmailManagement.Api.Controllers;

[ApiController]
[Route("api/v1/emails")]
[Authorize]
public class EmailController : ControllerBase
{
    private readonly IEmailEnqueueService _enqueueService;
    private readonly EmailQueryService _queryService;

    public EmailController(IEmailEnqueueService enqueueService, EmailQueryService queryService)
    {
        _enqueueService = enqueueService;
        _queryService = queryService;
    }

    [HttpPost("send")]
    [EnableRateLimiting("send-email")]
    [ProducesResponseType(typeof(ApiResponse<EnqueuedEmailResponse>), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Send([FromBody] SendEmailRequest request, CancellationToken ct)
    {
        var email = await _enqueueService.EnqueueAsync(
            request.TemplateKey, request.To, request.Cc, request.Bcc, request.Variables, ct);

        return StatusCode(202, new ApiResponse<EnqueuedEmailResponse>
        {
            Data = new EnqueuedEmailResponse
            {
                EmailId = email.EmailId,
                Status = email.Status,
                QueuedAt = email.QueuedAt,
            },
            Message = "Email has been queued for delivery.",
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<EmailListResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(
        [FromQuery] string? status,
        [FromQuery] string? templateKey,
        [FromQuery] string? recipient,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        EmailStatus? emailStatus = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<EmailStatus>(status, true, out var parsed))
            emailStatus = parsed;

        var result = await _queryService.QueryAsync(emailStatus, templateKey, recipient, dateFrom, dateTo, page, pageSize, ct);
        return Ok(new ApiResponse<EmailListResponse> { Data = result });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<EmailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var email = await _queryService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<EmailResponse> { Data = email });
    }

    [HttpGet("{id:guid}/events")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmailEventResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvents(Guid id, CancellationToken ct)
    {
        var events = await _queryService.GetEventsAsync(id, ct);
        return Ok(new ApiResponse<IReadOnlyList<EmailEventResponse>> { Data = events });
    }

    [HttpGet("{id:guid}/logs")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<EmailLogResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLogs(Guid id, CancellationToken ct)
    {
        var logs = await _queryService.GetLogsAsync(id, ct);
        return Ok(new ApiResponse<IReadOnlyList<EmailLogResponse>> { Data = logs });
    }
}