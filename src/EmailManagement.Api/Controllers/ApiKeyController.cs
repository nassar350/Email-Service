using EmailManagement.Application.DTOs.Requests;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailManagement.Api.Controllers;

[ApiController]
[Route("api/v1/api-keys")]
[Authorize]
public class ApiKeyController : ControllerBase
{
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyController(ApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApiKeyCreatedResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyRequest request, CancellationToken ct)
    {
        var result = await _apiKeyService.GenerateAsync(request.Name, request.ExpiresAt, ct);
        return StatusCode(201, new ApiResponse<ApiKeyCreatedResponse>
        {
            Data = result,
            Message = "API key created. Save the raw key — it will not be shown again.",
        });
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ApiKeyResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var keys = await _apiKeyService.GetAllAsync(ct);
        return Ok(new ApiResponse<IReadOnlyList<ApiKeyResponse>> { Data = keys });
    }

    [HttpPost("{id:guid}/revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Revoke(Guid id, CancellationToken ct)
    {
        await _apiKeyService.RevokeAsync(id, ct);
        return NoContent();
    }
}