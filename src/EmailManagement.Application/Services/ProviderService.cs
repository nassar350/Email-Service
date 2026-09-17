using EmailManagement.Application.DTOs.Requests;
using EmailManagement.Application.DTOs.Responses;
using EmailManagement.Application.Exceptions;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace EmailManagement.Application.Services;

public class ProviderService
{
    private readonly IProviderRepository _repository;
    private readonly IEmailProviderFactory _factory;
    private readonly IEnumerable<IEmailProviderDiagnostics> _diagnostics;
    private readonly ILogger<ProviderService> _logger;

    public ProviderService(
        IProviderRepository repository,
        IEmailProviderFactory factory,
        IEnumerable<IEmailProviderDiagnostics> diagnostics,
        ILogger<ProviderService> logger)
    {
        _repository = repository;
        _factory = factory;
        _diagnostics = diagnostics;
        _logger = logger;
    }

    public async Task<EmailProvider> CreateAsync(CreateProviderRequest request, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ProviderType>(request.ProviderType, true, out var providerType))
            throw new ValidationException(new List<ValidationError>
            {
                new("providerType", $"Invalid provider type: {request.ProviderType}.")
            });

        var provider = new EmailProvider
        {
            ProviderId = Guid.NewGuid(),
            ProviderType = providerType,
            ProviderName = request.ProviderName,
            Configuration = request.Configuration,
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        if (provider.IsDefault)
        {
            var existingDefault = await _repository.GetDefaultActiveAsync(providerType, ct);
            if (existingDefault is not null)
            {
                existingDefault.IsDefault = false;
                existingDefault.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(existingDefault, ct);
            }
        }

        await _repository.AddAsync(provider, ct);
        _logger.LogInformation("Provider created: {Name} ({Type})", provider.ProviderName, provider.ProviderType);
        return provider;
    }

    public async Task<EmailProvider> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(EmailProvider), id);
    }

    public async Task<IReadOnlyList<EmailProvider>> GetAllAsync(CancellationToken ct = default)
    {
        return await _repository.GetAllAsync(ct);
    }

    public async Task UpdateAsync(Guid id, UpdateProviderRequest request, CancellationToken ct = default)
    {
        var provider = await GetByIdAsync(id, ct);

        if (request.ProviderName is not null) provider.ProviderName = request.ProviderName;
        if (request.Configuration is not null) provider.Configuration = request.Configuration;
        if (request.IsDefault.HasValue && request.IsDefault.Value)
        {
            var existingDefault = await _repository.GetDefaultActiveAsync(provider.ProviderType, ct);
            if (existingDefault is not null && existingDefault.ProviderId != id)
            {
                existingDefault.IsDefault = false;
                existingDefault.UpdatedAt = DateTime.UtcNow;
                await _repository.UpdateAsync(existingDefault, ct);
            }
            provider.IsDefault = true;
        }
        if (request.IsActive.HasValue) provider.IsActive = request.IsActive.Value;
        provider.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(provider, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var provider = await GetByIdAsync(id, ct);

        if (await _repository.HasEmailsAsync(id, ct))
            throw new ConflictException("Cannot delete a provider that has associated emails. Deactivate it instead.");

        await _repository.DeleteAsync(provider, ct);
        _logger.LogInformation("Provider deleted: {Name}", provider.ProviderName);
    }

    public async Task<ProviderTestResponse> TestAsync(Guid id, CancellationToken ct = default)
    {
        var provider = await GetByIdAsync(id, ct);

        var diag = _diagnostics.FirstOrDefault(d =>
            d.ProviderType.Equals(provider.ProviderType.ToString(), StringComparison.OrdinalIgnoreCase));

        if (diag is null)
            return new ProviderTestResponse { Success = false, Message = $"No diagnostics available for provider type {provider.ProviderType}." };

        var result = await diag.TestAsync(ct);
        return new ProviderTestResponse { Success = result.Success, Message = result.Message };
    }
}