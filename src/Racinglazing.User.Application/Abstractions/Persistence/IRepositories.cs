using Racinglazing.User.Domain.Entities;
using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Application.Abstractions.Persistence;

/// <summary>
/// Reads and writes for the User aggregate. Lookups accept raw input and
/// normalise internally, so callers never need to know how uniqueness is
/// enforced.
/// </summary>
public interface IUserRepository
{
    Task<DomainUser?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads a user by id with roles and credential included.</summary>
    Task<DomainUser?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default);

    /// <summary>Loads by email (case-insensitive) with roles included.</summary>
    Task<DomainUser?> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <summary>Loads by username (case-insensitive) with roles included.</summary>
    Task<DomainUser?> GetByUsernameAsync(string username, CancellationToken ct = default);

    /// <summary>Loads by email or username (case-insensitive) with roles included.</summary>
    Task<DomainUser?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken ct = default);

    /// <summary>Loads the user linked to an external provider identity, if any.</summary>
    Task<DomainUser?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken ct = default);

    /// <summary>Batch profile lookup for other services (Forum/Race enrichment).</summary>
    Task<IReadOnlyList<DomainUser>> GetManyByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);

    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default);

    void Add(DomainUser user);
}

/// <summary>Reads roles for assignment and claim emission.</summary>
public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<IReadOnlyList<Role>> GetByNamesAsync(IReadOnlyCollection<string> names, CancellationToken ct = default);
}

/// <summary>Persistence for refresh tokens (rotation and revocation).</summary>
public interface IRefreshTokenRepository
{
    /// <summary>Finds a token by the hash of its raw value, with the owning user and roles.</summary>
    Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);

    /// <summary>All currently-active (not revoked, not expired) tokens for a user.</summary>
    Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default);

    void Add(RefreshToken token);
}
