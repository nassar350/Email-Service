using System.Security.Claims;
using System.Text.Encodings.Web;
using EmailManagement.Application.Ports;
using EmailManagement.Application.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EmailManagement.Api.Authentication;

public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private readonly ApiKeyService _apiKeyService;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ApiKeyService apiKeyService)
        : base(options, logger, encoder)
    {
        _apiKeyService = apiKeyService;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
            return AuthenticateResult.NoResult();

        var header = authHeader.ToString();
        if (!header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return AuthenticateResult.NoResult();

        var token = header["Bearer ".Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticateResult.NoResult();

        try
        {
            var principal = await _apiKeyService.ValidateAsync(token, Context.RequestAborted);
            if (principal is null)
                return AuthenticateResult.Fail("Invalid or inactive API key.");

            return AuthenticateResult.Success(
                new AuthenticationTicket(principal, Scheme.Name));
        }
        catch (Exception)
        {
            return AuthenticateResult.Fail("API key validation failed.");
        }
    }
}

public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions { }

public static class ApiKeyDefaults
{
    public const string AuthenticationScheme = "ApiKey";
}