using EmailManagement.Application.DTOs.Requests;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Services;
using EmailManagement.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EmailManagement.Api.Controllers;

[ApiController]
[Route("api/v1/providers")]
[Authorize]
public class ProviderController : ControllerBase
{
    private readonly ProviderService _providerService;

    public ProviderController(ProviderService providerService)
    {
        _providerService = providerService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ProviderResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var providers = await _providerService.GetAllAsync(ct);
        return Ok(new ApiResponse<IReadOnlyList<ProviderResponse>>
        {
            Data = providers.Select(MapToResponse).ToList(),
        });
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var provider = await _providerService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<ProviderResponse> { Data = MapToResponse(provider) });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProviderResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProviderRequest request, CancellationToken ct)
    {
        var provider = await _providerService.CreateAsync(request, ct);
        return StatusCode(201, new ApiResponse<ProviderResponse>
        {
            Data = MapToResponse(provider),
            Message = "Provider created.",
        });
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProviderResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProviderRequest request, CancellationToken ct)
    {
        await _providerService.UpdateAsync(id, request, ct);
        var provider = await _providerService.GetByIdAsync(id, ct);
        return Ok(new ApiResponse<ProviderResponse>
        {
            Data = MapToResponse(provider),
            Message = "Provider updated.",
        });
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _providerService.DeleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/test")]
    [ProducesResponseType(typeof(ApiResponse<ProviderTestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Test(Guid id, CancellationToken ct)
    {
        var result = await _providerService.TestAsync(id, ct);
        return Ok(new ApiResponse<ProviderTestResponse> { Data = result });
    }

    private static ProviderResponse MapToResponse(Domain.Entities.EmailProvider provider) => new()
    {
        ProviderId = provider.ProviderId,
        ProviderType = provider.ProviderType,
        ProviderName = provider.ProviderName,
        Configuration = provider.Configuration,
        IsDefault = provider.IsDefault,
        IsActive = provider.IsActive,
        CreatedAt = provider.CreatedAt,
        UpdatedAt = provider.UpdatedAt,
    };
}