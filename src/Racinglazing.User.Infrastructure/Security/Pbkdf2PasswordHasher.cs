using System.Security.Cryptography;
using Racinglazing.User.Application.Abstractions.Security;

namespace Racinglazing.User.Infrastructure.Security;

/// <summary>
/// Password hashing using PBKDF2-HMAC-SHA256. The stored value is
/// self-describing — <c>v1.{iterations}.{salt}.{hash}</c> — so the work factor
/// can be raised over time and older hashes upgraded transparently on the next
/// successful login (see <see cref="PasswordVerification.NeedsRehash"/>).
/// </summary>
/// <remarks>
/// PBKDF2 is used rather than bcrypt/argon2 to avoid a native dependency; the
/// iteration count follows OWASP guidance for PBKDF2-HMAC-SHA256. The interface
/// hides all of this, so swapping in Argon2 later is a one-class change.
/// </remarks>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string Prefix = "v1";
    private const int SaltSize = 16;   // 128-bit salt
    private const int KeySize = 32;    // 256-bit derived key
    private const int DefaultIterations = 210_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, DefaultIterations, Algorithm, KeySize);
        return string.Join('.', Prefix, DefaultIterations, Convert.ToBase64String(salt), Convert.ToBase64String(key));
    }

    public PasswordVerification Verify(string hash, string password)
    {
        var parts = hash.Split('.');
        if (parts.Length != 4 || parts[0] != Prefix) return PasswordVerification.Failed;
        if (!int.TryParse(parts[1], out var iterations) || iterations <= 0) return PasswordVerification.Failed;

        byte[] salt, expected;
        try
        {
            salt = Convert.FromBase64String(parts[2]);
            expected = Convert.FromBase64String(parts[3]);
        }
        catch (FormatException)
        {
            return PasswordVerification.Failed;
        }

        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expected.Length);

        // Constant-time comparison so a wrong password can't be timed byte-by-byte.
        if (!CryptographicOperations.FixedTimeEquals(actual, expected))
            return PasswordVerification.Failed;

        var needsRehash = iterations < DefaultIterations || expected.Length != KeySize;
        return new PasswordVerification(true, needsRehash);
    }
}
