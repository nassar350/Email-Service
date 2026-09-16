using EmailManagement.Api.Authentication;
using EmailManagement.Api.Bootstrap;
using EmailManagement.Api.Middleware;
using EmailManagement.Infrastructure;
using EmailManagement.Infrastructure.Configuration;
using EmailManagement.Infrastructure.Options;
using EmailManagement.Infrastructure.Providers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.OpenApi.Models;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

EnvFileLoader.LoadIfPresent();

builder.Configuration.AddEnvironmentVariables();

var mailjetConfig = builder.Configuration.GetSection(MailjetOptions.SectionName).Get<MailjetOptions>();
if (mailjetConfig is not null && !mailjetConfig.SkipStartupValidation)
{
    var errors = new List<string>();
    if (string.IsNullOrWhiteSpace(mailjetConfig.ApiKey))
        errors.Add("MAILJET_API_KEY is missing.");
    if (string.IsNullOrWhiteSpace(mailjetConfig.SecretKey))
        errors.Add("MAILJET_SECRET_KEY is missing.");
    if (errors.Count > 0)
        throw new InvalidOperationException(
            $"Missing required Mailjet configuration:\n{string.Join("\n", errors)}\n" +
            "Set MAILJET_SKIP_STARTUP_VALIDATION=true to bypass this check.");
}

builder.Services.AddEmailManagementInfrastructure(builder.Configuration);

builder.Services.AddAuthentication(ApiKeyDefaults.AuthenticationScheme)
    .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(
        ApiKeyDefaults.AuthenticationScheme, options => { });

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Email Management Service API",
        Version = "v1",
        Description = "A generic, reusable email management service with Mailjet integration.",
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "API Key",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter: Bearer ems_live_xxxxxxxxx",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer",
                },
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddRateLimiter(options =>
{
    var rateLimitConfig = builder.Configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
        ?? new RateLimitingOptions();

    options.AddFixedWindowLimiter("send-email", limiterOptions =>
    {
        limiterOptions.PermitLimit = rateLimitConfig.SendPermitLimit;
        limiterOptions.Window = TimeSpan.FromSeconds(rateLimitConfig.SendWindowSeconds);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddHostedService<BootstrapApiKeyHostedService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Email Management Service API v1");
    });
}

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }