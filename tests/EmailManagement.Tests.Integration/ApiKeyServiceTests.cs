using System.Security.Claims;
using EmailManagement.Application.Services;
using EmailManagement.Infrastructure.Security;
using EmailManagement.Tests.Integration.Fixtures;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EmailManagement.Tests.Integration;

public class ApiKeyServiceTests
{
    [Fact]
    public async Task GenerateThenValidate_RoundTrips()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var service = sp.GetRequiredService<ApiKeyService>();
        var hasher = sp.GetRequiredService<EmailManagement.Application.Ports.IApiKeyHasher>();

        var created = await service.GenerateAsync("integration", null);
        created.RawKey.Should().StartWith(ApiKeyService.KeyPrefix);

        var principal = await service.ValidateAsync(created.RawKey);

        principal.Should().NotBeNull();
        principal!.Identity!.IsAuthenticated.Should().BeTrue();
        principal.FindFirstValue("api_key_prefix").Should().Be(created.KeyPrefix);
    }

    [Fact]
    public async Task Validate_WrongKey_ReturnsNull()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ApiKeyService>();

        var principal = await service.ValidateAsync("ems_live_thiskeydoesnotexist");

        principal.Should().BeNull();
    }

    [Fact]
    public async Task Validate_NonEmsPrefix_ReturnsNull()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ApiKeyService>();

        var principal = await service.ValidateAsync("something_else");

        principal.Should().BeNull();
    }

    [Fact]
    public async Task Validate_RevokedKey_ReturnsNull()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var sp = scope.ServiceProvider;
        var service = sp.GetRequiredService<ApiKeyService>();
        var db = sp.GetRequiredService<Infrastructure.Persistence.ApplicationDbContext>();

        var created = await service.GenerateAsync("to-revoke", null);
        var stored = await db.ApiKeys.SingleAsync(k => k.ApiKeyId == created.ApiKeyId);
        stored.IsActive = false;
        stored.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var principal = await service.ValidateAsync(created.RawKey);

        principal.Should().BeNull();
    }

    [Fact]
    public async Task Bootstrap_CreatesOnlyWhenEmpty()
    {
        using var host = TestHost.Create();
        using var scope = host.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<ApiKeyService>();

        var first = await service.BootstrapIfEmptyAsync();
        first.RawKey.Should().NotBeNullOrEmpty();

        var second = await service.BootstrapIfEmptyAsync();

        second.Should().BeNull();
    }
}