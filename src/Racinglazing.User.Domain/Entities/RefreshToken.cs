namespace Racinglazing.User.Domain.Entities;

/// <summary>
/// A long-lived, opaque credential used to obtain new access tokens without
/// re-entering a password. The raw value is never stored — only a SHA-256 hash
/// — so a database leak does not hand an attacker usable tokens.
/// </summary>
/// <remarks>
/// Tokens are rotated on every use: refreshing revokes the presented token and
/// records the hash of its successor in <see cref="ReplacedByTokenHash"/>. If a
/// token that was already rotated is presented again, that is a replay — the
/// whole chain can be revoked. This is the standard refresh-token-rotation
/// defence against stolen tokens.
/// </remarks>
public sealed class RefreshToken
{
    private RefreshToken() { }

    internal RefreshToken(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, string? createdByIp)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        TokenHash = tokenHash;
        ExpiresAt = expiresAt;
        CreatedAt = createdAt;
        CreatedByIp = createdByIp;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }

    /// <summary>The owning user, loaded by the repository on refresh.</summary>
    public User? User { get; private set; }

    /// <summary>SHA-256 hash (hex) of the opaque token value.</summary>
    public string TokenHash { get; private set; } = null!;

    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? CreatedByIp { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }
    public string? RevokedByIp { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive(DateTimeOffset now) => !IsRevoked && !IsExpired(now);

    /// <summary>Revokes this token, optionally naming the token that replaces it.</summary>
    public void Revoke(DateTimeOffset now, string? revokedByIp, string? replacedByTokenHash = null)
    {
        if (IsRevoked) return;
        RevokedAt = now;
        RevokedByIp = revokedByIp;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
