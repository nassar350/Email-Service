using EmailManagement.Infrastructure;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EmailManagement.Tests.Integration.Fixtures;

public sealed class TestHost : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private TestHost(SqliteConnection connection, ServiceProvider provider)
    {
        _connection = connection;
        _provider = provider;
    }

    public static TestHost Create(Action<IServiceCollection>? overrideServices = null)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DATABASE_CONNECTION_STRING"] = "ignored-for-sqlite",
                ["Mailjet:SkipStartupValidation"] = "true",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEmailManagementInfrastructure(config);

        services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
        services.RemoveAll<IDbContextPool<ApplicationDbContext>>();
        services.RemoveAll<IScopedDbContextLease<ApplicationDbContext>>();
        services.RemoveAll<ApplicationDbContext>();
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(connection));

        overrideServices?.Invoke(services);

        var provider = services.BuildServiceProvider();

        using (var scope = provider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }

        return new TestHost(connection, provider);
    }

    public IServiceScope CreateScope() => _provider.CreateScope();

    public void Dispose()
    {
        _provider.Dispose();
        _connection.Dispose();
    }
}