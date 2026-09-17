using EmailManagement.Infrastructure.Configuration;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EmailManagement.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        EnvFileLoader.LoadIfPresent();

        var connStr = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? throw new InvalidOperationException(
                "DATABASE_CONNECTION_STRING environment variable is not set. " +
                "Configure it in .env or set the environment variable before running migrations.");

        optionsBuilder.UseMySql(connStr, new MySqlServerVersion(new Version(8, 0, 36)));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}