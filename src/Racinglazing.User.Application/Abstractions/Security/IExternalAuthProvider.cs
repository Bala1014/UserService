namespace Racinglazing.User.Application.Abstractions.Security;

/// <summary>
/// Validates an identity asserted by an external provider (Google today; Auth0,
/// Apple, GitHub, … tomorrow). Each provider is a separate implementation
/// keyed by <see cref="Provider"/>; <see cref="IExternalAuthProviderResolver"/>
/// picks the right one, so adding a provider never touches the sign-in use case.
/// </summary>
public interface IExternalAuthProvider
{
    /// <summary>Stable discriminator, e.g. "google".</summary>
    string Provider { get; }

    /// <summary>
    /// Verifies the provider-issued credential (for Google, the OIDC id_token)
    /// and returns the identity it asserts, or null when the credential is
    /// invalid or cannot be verified.
    /// </summary>
    Task<ExternalUserInfo?> ValidateAsync(string credential, CancellationToken ct = default);
}

/// <summary>Resolves the <see cref="IExternalAuthProvider"/> for a provider name.</summary>
public interface IExternalAuthProviderResolver
{
    IExternalAuthProvider? Resolve(string provider);
}

/// <summary>Identity details asserted by an external provider.</summary>
public sealed record ExternalUserInfo(
    string Provider,
    string Subject,
    string Email,
    bool EmailVerified,
    string? Name,
    string? PictureUrl);
