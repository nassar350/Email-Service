using System.Net;
using System.Net.Http.Headers;
using System.Text;
using EmailManagement.Application.Ports;
using EmailManagement.Application.Services;
using EmailManagement.Infrastructure.Options;
using EmailManagement.Infrastructure.Persistence;
using EmailManagement.Infrastructure.Persistence.Repositories;
using EmailManagement.Infrastructure.Providers;
using EmailManagement.Infrastructure.Security;
using EmailManagement.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EmailManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddEmailManagementInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        bool useSqlite = false,
        string? sqliteConnectionString = null)
    {
        services.AddDbContextPool<ApplicationDbContext>(options =>
        {
            if (useSqlite)
            {
                options.UseSqlite(sqliteConnectionString ?? "Data Source=:memory:");
            }
            else
            {
                var connStr = configuration["DATABASE_CONNECTION_STRING"]
                    ?? configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Database connection string is not configured.");

                options.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                        mySqlOptions.CommandTimeout(30);
                    });
            }

            options.EnableSensitiveDataLogging(false);
            options.EnableDetailedErrors(false);
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IApiKeyRepository, ApiKeyRepository>();
        services.AddScoped<ITemplateRepository, TemplateRepository>();
        services.AddScoped<ITemplateVersionRepository, TemplateVersionRepository>();
        services.AddScoped<IProviderRepository, ProviderRepository>();
        services.AddScoped<IEmailRepository, EmailRepository>();
        services.AddScoped<IEmailRecipientRepository, EmailRecipientRepository>();
        services.AddScoped<IEmailEventRepository, EmailEventRepository>();
        services.AddScoped<IEmailLogRepository, EmailLogRepository>();
        services.AddScoped<IApiKeyHasher, ApiKeyHasher>();

        services.AddSingleton<ITemplateRenderer, TemplateRenderer>();
        services.AddSingleton<IFileStorage>(sp =>
            new LocalFileStorage(
                configuration["STORAGE_PATH"]
                ?? Path.Combine(AppContext.BaseDirectory, "storage")));

        services.Configure<MailjetOptions>(configuration.GetSection(MailjetOptions.SectionName));
        services.Configure<WorkerOptions>(configuration.GetSection(WorkerOptions.SectionName));
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        services.AddHttpClient("mailjet", (sp, client) =>
        {
            var options = configuration.GetSection(MailjetOptions.SectionName).Get<MailjetOptions>()
                ?? new MailjetOptions();

            var credentials = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{options.ApiKey}:{options.SecretKey}"));

            client.BaseAddress = new Uri("https://api.mailjet.com");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddSingleton<IEmailProvider, MailjetEmailProvider>();
        services.AddSingleton<IEmailProviderDiagnostics>(sp => (MailjetEmailProvider)sp.GetRequiredService<IEmailProvider>());
        services.AddSingleton<IEmailProviderFactory, EmailProviderFactory>();

        services.AddScoped<ApiKeyService>();
        services.AddScoped<TemplateService>();
        services.AddScoped<ProviderService>();
        services.AddScoped<IEmailEnqueueService, EmailEnqueueService>();
        services.AddScoped<EmailQueryService>();
        services.AddScoped<EmailProcessingService>();
        services.AddScoped<EmailWebhookService>();

        services.AddSingleton<MailjetWebhookAuthenticator>();

        return services;
    }
}