namespace EmailManagement.Application.Ports;

public interface IApiKeyHasher
{
    string Hash(string rawKey, string prefix);
}