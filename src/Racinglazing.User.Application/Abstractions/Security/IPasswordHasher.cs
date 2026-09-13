namespace Racinglazing.User.Application.Abstractions.Security;

/// <summary>
/// One-way hashing and verification of passwords. The algorithm and its
/// parameters are an implementation detail — callers only ever hand over a
/// plaintext to hash or a stored hash to verify against.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>Produces a self-describing hash (algorithm + parameters + salt + digest).</summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext against a stored hash. <see cref="PasswordVerification.NeedsRehash"/>
    /// signals that the stored hash used weaker parameters than the current policy
    /// and should be transparently upgraded on this successful login.
    /// </summary>
    PasswordVerification Verify(string hash, string password);
}

/// <summary>Outcome of a password check.</summary>
public readonly record struct PasswordVerification(bool Succeeded, bool NeedsRehash)
{
    public static readonly PasswordVerification Failed = new(false, false);
}
