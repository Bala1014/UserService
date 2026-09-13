using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Racinglazing.User.Infrastructure.Security;

namespace Racinglazing.User.Api.Controllers;

/// <summary>
/// OpenID-Connect-style discovery: the JWKS endpoint publishes the public
/// signing key, and the discovery document points at it. This is what lets
/// RaceService and ForumService set <c>Jwt:Authority</c> to this service and
/// validate tokens by fetching the public key — no shared secret, automatic
/// rotation. Only meaningful for RS256; for HS256 there is nothing to publish.
/// </summary>
[ApiController]
public sealed class DiscoveryController(
    IJwtKeyProvider keyProvider,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    /// <summary>The OIDC discovery document.</summary>
    [HttpGet("/.well-known/openid-configuration")]
    public IActionResult OpenIdConfiguration()
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(new Dictionary<string, object>
        {
            ["issuer"] = _jwt.Issuer,
            ["jwks_uri"] = $"{baseUrl}/.well-known/jwks.json",
            ["authorization_endpoint"] = $"{baseUrl}/api/v1/auth/login",
            ["token_endpoint"] = $"{baseUrl}/api/v1/auth/login",
            ["response_types_supported"] = new[] { "token" },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { keyProvider.IsAsymmetric ? "RS256" : "HS256" },
            ["grant_types_supported"] = new[] { "password", "refresh_token" },
            ["claims_supported"] = new[] { "sub", "email", "email_verified", "preferred_username", "name", "role" }
        });
    }

    /// <summary>The JSON Web Key Set — the public key(s) for signature verification.</summary>
    [HttpGet("/.well-known/jwks.json")]
    public IActionResult Jwks()
    {
        var keys = keyProvider.PublicKeys.Select(k => new Dictionary<string, string?>
        {
            ["kty"] = k.Kty,
            ["use"] = k.Use,
            ["kid"] = k.Kid,
            ["alg"] = k.Alg,
            ["n"] = k.N,
            ["e"] = k.E
        });
        return Ok(new { keys });
    }
}
