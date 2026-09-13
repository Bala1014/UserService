using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Application.Abstractions.Security;

/// <summary>
/// Mints signed access tokens (JWTs) for authenticated users. The signing
/// scheme (symmetric HS256 for local dev, asymmetric RS256 + JWKS for
/// production) is chosen by configuration inside the implementation — the
/// application layer is indifferent to it.
/// </summary>
public interface ITokenService
{
    AccessToken CreateAccessToken(DomainUser user, IReadOnlyCollection<string> roles);
}

/// <summary>A minted access token and its lifetime.</summary>
public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt, int ExpiresInSeconds);

/// <summary>
/// Generates opaque refresh tokens and the hash under which they are stored.
/// The raw value is returned to the client exactly once; only the hash is ever
/// persisted.
/// </summary>
public interface IRefreshTokenGenerator
{
    /// <summary>Creates a new cryptographically-random token and its storage hash.</summary>
    (string Raw, string Hash) Create();

    /// <summary>Hashes a presented raw token so it can be matched against storage.</summary>
    string Hash(string raw);
}
