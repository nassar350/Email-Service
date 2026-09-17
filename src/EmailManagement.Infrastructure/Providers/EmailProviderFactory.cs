using EmailManagement.Application.Ports;
using EmailManagement.Domain.Enums;

namespace EmailManagement.Infrastructure.Providers;

public class EmailProviderFactory : IEmailProviderFactory
{
    private readonly Dictionary<string, IEmailProvider> _providers;

    public EmailProviderFactory(IEnumerable<IEmailProvider> providers)
    {
        _providers = providers.ToDictionary(p => p.ProviderType, p => p, StringComparer.OrdinalIgnoreCase);
    }

    public IEmailProvider GetProvider(ProviderType providerType)
    {
        var key = providerType.ToString();
        if (_providers.TryGetValue(key, out var provider))
            return provider;

        throw new NotSupportedException($"Provider type '{key}' is not supported.");
    }
}