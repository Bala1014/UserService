namespace Racinglazing.User.Infrastructure.Security;

/// <summary>
/// JWT issuance settings, bound from the "Jwt" configuration section. The same
/// section name, issuer and audiences that RaceService and ForumService already
/// expect to validate against — this is the contract between the services.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>The <c>iss</c> claim. Must match the consumers' <c>Jwt:Issuer</c>.</summary>
    public string Issuer { get; set; } = "racingvacing-userservice";

    /// <summary>
    /// The <c>aud</c> values placed on every access token. Including every
    /// service's audience lets one token be presented to any of them.
    /// </summary>
    public string[] Audiences { get; set; } =
        ["racingvacing-forumservice", "racingvacing-raceservice", "racingvacing-userservice"];

    public int AccessTokenLifetimeMinutes { get; set; } = 15;

    /// <summary>"RS256" (asymmetric, JWKS-verifiable) or "HS256" (shared secret).</summary>
    public string Algorithm { get; set; } = "RS256";

    /// <summary>
    /// PEM-encoded RSA private key for RS256. When empty, an ephemeral key is
    /// generated at startup — fine for a single dev instance, but tokens won't
    /// survive a restart and multiple instances won't agree, so production must
    /// supply one (and distribute only the public half, via the JWKS endpoint).
    /// </summary>
    public string? PrivateKeyPem { get; set; }

    /// <summary>Shared secret for HS256 (local-dev convenience only).</summary>
    public string? SigningKey { get; set; }

    /// <summary>Optional stable key id surfaced in the JWT header and JWKS.</summary>
    public string? KeyId { get; set; }
}
