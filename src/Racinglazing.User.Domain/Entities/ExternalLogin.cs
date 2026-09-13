namespace Racinglazing.User.Domain.Entities;

/// <summary>
/// A link between a local account and an identity held by an external provider
/// (e.g. Google). <see cref="Provider"/> + <see cref="ProviderKey"/> (the
/// provider's stable subject id) is unique, so the same Google account can only
/// be attached to one local user.
/// </summary>
public sealed class ExternalLogin
{
    private ExternalLogin() { }

    public ExternalLogin(Guid userId, string provider, string providerKey, DateTimeOffset createdAt)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Provider = provider;
        ProviderKey = providerKey;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>Provider discriminator, e.g. "google".</summary>
    public string Provider { get; private set; } = null!;

    /// <summary>The provider's immutable subject identifier for the user.</summary>
    public string ProviderKey { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }
}
