using System.Security.Cryptography;
using System.Text;
using Racinglazing.User.Application.Abstractions.Security;

namespace Racinglazing.User.Infrastructure.Security;

/// <summary>
/// Generates 256 bits of cryptographic randomness as the refresh token, and
/// stores only its SHA-256 hash. The raw value is opaque to everyone including
/// this service after issuance — verification is by hashing the presented value
/// and matching, exactly like a password.
/// </summary>
public sealed class RefreshTokenGenerator : IRefreshTokenGenerator
{
    private const int TokenSizeBytes = 32;

    public (string Raw, string Hash) Create()
    {
        var raw = Base64UrlEncode(RandomNumberGenerator.GetBytes(TokenSizeBytes));
        return (raw, Hash(raw));
    }

    public string Hash(string raw)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(bytes);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
