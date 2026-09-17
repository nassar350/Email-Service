using System.Net;
using System.Text.Json;
using EmailManagement.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using ApplicationValidationException = EmailManagement.Application.Exceptions.ValidationException;

namespace EmailManagement.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        httpContext.Response.ContentType = "application/json";

        var (statusCode, message, errors) = exception switch
        {
            ApplicationValidationException ex =>
                (HttpStatusCode.BadRequest, ex.Message, ex.Errors.Select(e => new { field = e.Field, message = e.Message }).ToList()),
            FluentValidation.ValidationException ex =>
                (HttpStatusCode.BadRequest, "Validation failed.", ex.Errors.Select(e => new { field = e.PropertyName, message = e.ErrorMessage }).ToList()),
            NotFoundException ex =>
                (HttpStatusCode.NotFound, ex.Message, (object?)null),
            ConflictException ex =>
                (HttpStatusCode.Conflict, ex.Message, (object?)null),
            ConfigurationException ex =>
                (HttpStatusCode.InternalServerError, "Configuration error.", new[] { new { field = "configuration", message = ex.Message } }),
            _ =>
                (HttpStatusCode.InternalServerError, "An unexpected error occurred.", (object?)null),
        };

        if (statusCode == HttpStatusCode.InternalServerError && exception is not ConfigurationException)
        {
            _logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = (int)statusCode;

        var response = new
        {
            status = (int)statusCode,
            message,
            errors,
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        await httpContext.Response.WriteAsync(JsonSerializer.Serialize(response, options), ct);
        return true;
    }
}