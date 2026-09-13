using Racinglazing.User.Domain.Enums;

namespace Racinglazing.User.Domain.Entities;

/// <summary>
/// A person who can sign in and be referenced by the other services. This is
/// the aggregate root for identity: it owns its credential, its role
/// assignments and its external-provider links.
/// </summary>
/// <remarks>
/// Email and username are stored both as entered (for display) and normalised
/// (upper-invariant, for lookup and uniqueness), mirroring the approach ASP.NET
/// Identity takes. Uniqueness is enforced on the normalised columns so
/// "Ada@x.com" and "ada@x.com" cannot both register. State transitions go
/// through methods rather than public setters so an invariant lives in exactly
/// one place.
/// </remarks>
public sealed class User
{
    private readonly List<UserRole> _roles = [];
    private readonly List<ExternalLogin> _externalLogins = [];
    private readonly List<RefreshToken> _refreshTokens = [];

    // EF materialisation.
    private User() { }

    private User(Guid id, string email, string username, string displayName, DateTimeOffset now)
    {
        Id = id;
        Email = email;
        NormalizedEmail = Normalize(email);
        Username = username;
        NormalizedUsername = Normalize(username);
        DisplayName = displayName;
        Status = UserStatus.Active;
        CreatedAt = now;
        UpdatedAt = now;
        SecurityStamp = Guid.NewGuid().ToString("N");
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = null!;
    public string NormalizedEmail { get; private set; } = null!;
    public bool EmailVerified { get; private set; }

    public string Username { get; private set; } = null!;
    public string NormalizedUsername { get; private set; } = null!;

    public string DisplayName { get; private set; } = null!;
    public string? AvatarUrl { get; private set; }
    public string? Bio { get; private set; }

    /// <summary>
    /// PBKDF2 hash of the password, or null for an account that only signs in
    /// through an external provider (e.g. Google).
    /// </summary>
    public string? PasswordHash { get; private set; }

    public UserStatus Status { get; private set; }

    /// <summary>
    /// Rotated whenever the credential materially changes (password reset,
    /// account disabled). Embedding it in issued tokens lets every service
    /// invalidate outstanding sessions without a shared revocation list.
    /// </summary>
    public string SecurityStamp { get; private set; } = null!;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles;
    public IReadOnlyCollection<ExternalLogin> ExternalLogins => _externalLogins;
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens;

    /// <summary>Creates a locally-authenticated account (email + password).</summary>
    public static User Register(string email, string username, string displayName, string passwordHash, DateTimeOffset now)
    {
        var user = new User(Guid.NewGuid(), email, username, displayName, now)
        {
            PasswordHash = passwordHash
        };
        return user;
    }

    /// <summary>
    /// Creates an account provisioned from an external provider. It has no
    /// password until the user chooses to set one.
    /// </summary>
    public static User RegisterExternal(
        string email, string username, string displayName, bool emailVerified,
        string provider, string providerKey, DateTimeOffset now)
    {
        var user = new User(Guid.NewGuid(), email, username, displayName, now)
        {
            EmailVerified = emailVerified
        };
        user.LinkExternalLogin(provider, providerKey, now);
        return user;
    }

    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        Touch(now);
        RotateSecurityStamp();
    }

    /// <summary>
    /// Replaces the stored hash with one computed under stronger parameters,
    /// without rotating the security stamp — the credential itself is unchanged,
    /// so existing sessions stay valid.
    /// </summary>
    public void UpgradePasswordHash(string passwordHash, DateTimeOffset now)
    {
        PasswordHash = passwordHash;
        Touch(now);
    }

    public bool HasPassword => !string.IsNullOrEmpty(PasswordHash);

    public void UpdateProfile(string displayName, string? avatarUrl, string? bio, DateTimeOffset now)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        Bio = bio;
        Touch(now);
    }

    public void MarkEmailVerified(DateTimeOffset now)
    {
        if (EmailVerified) return;
        EmailVerified = true;
        Touch(now);
    }

    public void RecordLogin(DateTimeOffset now) => LastLoginAt = now;

    public void Disable(DateTimeOffset now)
    {
        Status = UserStatus.Disabled;
        Touch(now);
        RotateSecurityStamp();
    }

    public void Reinstate(DateTimeOffset now)
    {
        Status = UserStatus.Active;
        Touch(now);
    }

    public bool CanAuthenticate => Status == UserStatus.Active;

    public void AssignRole(Role role)
    {
        if (_roles.Any(r => r.RoleId == role.Id)) return;
        _roles.Add(new UserRole(Id, role.Id));
    }

    public void LinkExternalLogin(string provider, string providerKey, DateTimeOffset now)
    {
        if (_externalLogins.Any(l => l.Provider == provider && l.ProviderKey == providerKey)) return;
        _externalLogins.Add(new ExternalLogin(Id, provider, providerKey, now));
    }

    /// <summary>Issues a new refresh token for this user's active session.</summary>
    public RefreshToken IssueRefreshToken(string tokenHash, DateTimeOffset expiresAt, DateTimeOffset now, string? createdByIp)
    {
        var token = new RefreshToken(Id, tokenHash, expiresAt, now, createdByIp);
        _refreshTokens.Add(token);
        return token;
    }

    private void Touch(DateTimeOffset now) => UpdatedAt = now;
    private void RotateSecurityStamp() => SecurityStamp = Guid.NewGuid().ToString("N");

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
