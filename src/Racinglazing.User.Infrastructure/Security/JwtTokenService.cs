using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Racinglazing.User.Application.Abstractions.Security;
using Racinglazing.User.Application.Abstractions.Services;
using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Infrastructure.Security;

/// <summary>
/// Mints access tokens as JWTs. The claim names are chosen to match what the
/// other services read: <c>sub</c> for the user id and <c>role</c> for each
/// role (RaceService sets NameClaimType=sub, RoleClaimType=role with inbound
/// mapping disabled). Standard OIDC claims (<c>email</c>,
/// <c>preferred_username</c>, <c>name</c>) are included for convenience.
/// </summary>
public sealed class JwtTokenService(
    IJwtKeyProvider keyProvider,
    IOptions<JwtOptions> options,
    IDateTimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;
    private readonly JsonWebTokenHandler _handler = new();

    public AccessToken CreateAccessToken(DomainUser user, IReadOnlyCollection<string> roles)
    {
        var now = clock.UtcNow;
        var expires = now.AddMinutes(_options.AccessTokenLifetimeMinutes);

        var claims = new List<Claim>
        {
            new("sub", user.Id.ToString()),
            new("email", user.Email),
            new("email_verified", user.EmailVerified ? "true" : "false", ClaimValueTypes.Boolean),
            new("preferred_username", user.Username),
            new("name", user.DisplayName),
            // Lets every service invalidate a session when the credential changes,
            // without a shared revocation list.
            new("security_stamp", user.SecurityStamp),
            new("jti", Guid.NewGuid().ToString("N"))
        };
        claims.AddRange(roles.Select(r => new Claim("role", r)));

        // Emit each audience as an `aud` claim rather than via
        // SecurityTokenDescriptor.Audiences: the latter duplicates entries in
        // Microsoft.IdentityModel 8.14. The resulting `aud` is still an array,
        // which is what the consumers validate against.
        claims.AddRange(_options.Audiences.Distinct().Select(a => new Claim("aud", a)));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expires.UtcDateTime,
            SigningCredentials = keyProvider.SigningCredentials,
            TokenType = "at+jwt"
        };

        var token = _handler.CreateToken(descriptor);
        return new AccessToken(token, expires, _options.AccessTokenLifetimeMinutes * 60);
    }
}
