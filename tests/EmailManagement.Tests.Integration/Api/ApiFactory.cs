using System.Text;
using EmailManagement.Api;
using EmailManagement.Application.Ports;
using EmailManagement.Domain.Enums;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Moq.Protected;

namespace EmailManagement.Tests.Integration.Api;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly Queue<HttpResponseMessage> _mailjetResponses = new();

    public ApiFactory ConfigureMailjet(Queue<HttpResponseMessage> responses)
    {
        _mailjetResponses.Clear();
        foreach (var response in responses)
            _mailjetResponses.Enqueue(response);
        return this;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _connection.Open();

        builder.UseSetting("Mailjet:SkipStartupValidation", "true");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextPool<ApplicationDbContext>>();
            services.RemoveAll<IScopedDbContextLease<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(_connection));

            using (var scope = services.BuildServiceProvider(validateScopes: false).CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Database.EnsureCreated();
            }

            if (_mailjetResponses.Count > 0)
            {
                var handler = new Mock<HttpMessageHandler>();
                handler.Protected()
                    .Setup<Task<HttpResponseMessage>>("SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>())
                    .ReturnsAsync(() => _mailjetResponses.Dequeue());

                var factory = new Mock<IHttpClientFactory>();
                factory.Setup(f => f.CreateClient(It.IsAny<string>()))
                    .Returns(() => new HttpClient(handler.Object) { BaseAddress = new Uri("https://api.mailjet.com") });

                services.RemoveAll<IHttpClientFactory>();
                services.AddSingleton(factory.Object);
            }
        });
    }

    public async Task<ApiKeyRecord> CreateApiKeyAsync()
    {
        using var scope = Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<EmailManagement.Application.Services.ApiKeyService>();
        var created = await service.GenerateAsync("integration-test", null);
        return new ApiKeyRecord(created.RawKey);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }

    public sealed record ApiKeyRecord(string RawKey);
}