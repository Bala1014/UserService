using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Racinglazing.User.Infrastructure.Security;

/// <summary>
/// Holds the signing material for issued tokens and, for asymmetric signing,
/// exposes the public half as a JWK so other services can verify signatures
/// without sharing a secret. Registered as a singleton: the key is resolved
/// once at startup and reused.
/// </summary>
public interface IJwtKeyProvider
{
    SigningCredentials SigningCredentials { get; }
    string Algorithm { get; }

    /// <summary>True for RS256 (JWKS-publishable); false for HS256.</summary>
    bool IsAsymmetric { get; }

    /// <summary>Public keys for the JWKS endpoint. Empty for symmetric signing.</summary>
    IReadOnlyList<JsonWebKey> PublicKeys { get; }
}

/// <inheritdoc />
public sealed class JwtKeyProvider : IJwtKeyProvider
{
    public JwtKeyProvider(IOptions<JwtOptions> options, ILogger<JwtKeyProvider> logger)
    {
        var o = options.Value;
        var algorithm = o.Algorithm?.Trim().ToUpperInvariant();

        if (algorithm == "HS256")
        {
            if (string.IsNullOrWhiteSpace(o.SigningKey) || o.SigningKey.Length < 32)
                throw new InvalidOperationException(
                    "Jwt:SigningKey must be at least 32 characters when Jwt:Algorithm is HS256.");

            var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(o.SigningKey))
            {
                KeyId = o.KeyId ?? "hs256-local"
            };
            Algorithm = SecurityAlgorithms.HmacSha256;
            IsAsymmetric = false;
            SigningCredentials = new SigningCredentials(key, Algorithm);
            PublicKeys = [];
            logger.LogWarning("JWT signing is using a symmetric HS256 key — intended for local development only.");
            return;
        }

        // Default: RS256 (asymmetric).
        var rsa = RSA.Create(2048);
        if (!string.IsNullOrWhiteSpace(o.PrivateKeyPem))
        {
            rsa.ImportFromPem(o.PrivateKeyPem);
        }
        else
        {
            logger.LogWarning(
                "No Jwt:PrivateKeyPem configured — generated an ephemeral RS256 key. Tokens will not survive a restart " +
                "and multiple instances will not agree. Supply a persistent key in non-dev environments.");
        }

        var keyId = o.KeyId ?? Base64Url(SHA256.HashData(rsa.ExportRSAPublicKey())[..8]);
        var privateKey = new RsaSecurityKey(rsa) { KeyId = keyId };

        Algorithm = SecurityAlgorithms.RsaSha256;
        IsAsymmetric = true;
        SigningCredentials = new SigningCredentials(privateKey, Algorithm);

        // Publish only the public half.
        var publicRsa = RSA.Create();
        publicRsa.ImportParameters(rsa.ExportParameters(false));
        var publicKey = new RsaSecurityKey(publicRsa) { KeyId = keyId };
        var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(publicKey);
        jwk.Use = "sig";
        jwk.Alg = Algorithm;
        PublicKeys = [jwk];
    }

    public SigningCredentials SigningCredentials { get; }
    public string Algorithm { get; }
    public bool IsAsymmetric { get; }
    public IReadOnlyList<JsonWebKey> PublicKeys { get; }

    private static string Base64Url(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
