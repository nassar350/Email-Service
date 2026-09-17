using EmailManagement.Application.Services;
using EmailManagement.Infrastructure.Providers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailManagement.Api.Controllers;

[ApiController]
[Route("api/v1/webhooks")]
[AllowAnonymous]
public class WebhookController : ControllerBase
{
    private readonly MailjetWebhookAuthenticator _authenticator;
    private readonly EmailWebhookService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        MailjetWebhookAuthenticator authenticator,
        EmailWebhookService webhookService,
        ILogger<WebhookController> logger)
    {
        _authenticator = authenticator;
        _webhookService = webhookService;
        _logger = logger;
    }

    [HttpPost("mailjet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Mailjet(CancellationToken ct)
    {
        if (!_authenticator.Authenticate(HttpContext.Request.Headers.Authorization.ToString()))
            return Unauthorized();

        try
        {
            using var reader = new StreamReader(HttpContext.Request.Body);
            var body = await reader.ReadToEndAsync(ct);

            if (string.IsNullOrWhiteSpace(body))
                return BadRequest("Empty request body.");

            _logger.LogInformation("Mailjet webhook received");

            var events = MailjetWebhookMapper.MapEvents(body);
            if (events.Count == 0)
                return Ok("No valid events found.");

            await _webhookService.ProcessEventsAsync(events, ct);

            _logger.LogInformation("Mailjet webhook processed {Count} events", events.Count);
            return Ok("Events processed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Mailjet webhook");
            return StatusCode(500, "Internal error processing webhook.");
        }
    }
}