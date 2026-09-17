using System.Security.Cryptography;
using System.Text;
using EmailManagement.Application.Ports;

namespace EmailManagement.Infrastructure.Security;

public class ApiKeyHasher : IApiKeyHasher
{
    public string Hash(string rawKey, string prefix)
    {
        var salted = $"{prefix}.{rawKey}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(salted));
        return Convert.ToHexString(bytes);
    }
}