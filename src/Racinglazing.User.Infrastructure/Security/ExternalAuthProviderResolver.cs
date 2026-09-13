using Racinglazing.User.Application.Abstractions.Security;

namespace Racinglazing.User.Infrastructure.Security;

/// <summary>
/// Picks the external provider for a given name from whatever providers are
/// registered. A provider that isn't configured simply isn't registered, so
/// resolution returns null and the use case reports it as unavailable.
/// </summary>
public sealed class ExternalAuthProviderResolver : IExternalAuthProviderResolver
{
    private readonly IReadOnlyDictionary<string, IExternalAuthProvider> _providers;

    public ExternalAuthProviderResolver(IEnumerable<IExternalAuthProvider> providers)
        => _providers = providers.ToDictionary(p => p.Provider, StringComparer.OrdinalIgnoreCase);

    public IExternalAuthProvider? Resolve(string provider)
        => _providers.TryGetValue(provider, out var p) ? p : null;
}
