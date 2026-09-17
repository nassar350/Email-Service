using EmailManagement.Infrastructure.Security;
using FluentAssertions;

namespace EmailManagement.Tests.Unit.Infrastructure;

public class ApiKeyHasherTests
{
    private readonly ApiKeyHasher _hasher = new();

    [Fact]
    public void Hash_IsDeterministic()
    {
        _hasher.Hash("abc", "p1").Should().Be(_hasher.Hash("abc", "p1"));
    }

    [Fact]
    public void Hash_DifferentKey_DifferentHash()
    {
        _hasher.Hash("abc", "p1").Should().NotBe(_hasher.Hash("abd", "p1"));
    }

    [Fact]
    public void Hash_DifferentPrefix_DifferentHash()
    {
        _hasher.Hash("abc", "p1").Should().NotBe(_hasher.Hash("abc", "p2"));
    }

    [Fact]
    public void Hash_ReturnsHexStringOfProperLength()
    {
        _hasher.Hash("abc", "p1").Should().MatchRegex("^[0-9A-F]{64}$");
    }

    [Fact]
    public void Hash_DoesNotExposeRawKey()
    {
        _hasher.Hash("super-secret-key", "p1").Should().NotContain("super-secret-key");
    }
}