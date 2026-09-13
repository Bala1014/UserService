using Microsoft.EntityFrameworkCore;
using Racinglazing.User.Application.Abstractions.Persistence;
using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Infrastructure.Persistence.Repositories;

public sealed class UserRepository(UserDbContext db) : IUserRepository
{
    public Task<DomainUser?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<DomainUser?> GetByIdWithRolesAsync(Guid id, CancellationToken ct = default)
        => WithRoles().FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<DomainUser?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = Normalize(email);
        return WithRoles().FirstOrDefaultAsync(u => u.NormalizedEmail == normalized, ct);
    }

    public Task<DomainUser?> GetByUsernameAsync(string username, CancellationToken ct = default)
    {
        var normalized = Normalize(username);
        return WithRoles().FirstOrDefaultAsync(u => u.NormalizedUsername == normalized, ct);
    }

    public Task<DomainUser?> GetByEmailOrUsernameAsync(string emailOrUsername, CancellationToken ct = default)
    {
        var normalized = Normalize(emailOrUsername);
        return WithRoles().FirstOrDefaultAsync(
            u => u.NormalizedEmail == normalized || u.NormalizedUsername == normalized, ct);
    }

    public Task<DomainUser?> GetByExternalLoginAsync(string provider, string providerKey, CancellationToken ct = default)
        => WithRoles().FirstOrDefaultAsync(
            u => u.ExternalLogins.Any(l => l.Provider == provider && l.ProviderKey == providerKey), ct);

    public async Task<IReadOnlyList<DomainUser>> GetManyByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return [];
        return await db.Users.Where(u => ids.Contains(u.Id)).ToListAsync(ct);
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = Normalize(email);
        return db.Users.AnyAsync(u => u.NormalizedEmail == normalized, ct);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken ct = default)
    {
        var normalized = Normalize(username);
        return db.Users.AnyAsync(u => u.NormalizedUsername == normalized, ct);
    }

    public void Add(DomainUser user) => db.Users.Add(user);

    // Role names are needed wherever we emit claims or render a profile.
    private IQueryable<DomainUser> WithRoles()
        => db.Users.Include(u => u.Roles).ThenInclude(r => r.Role);

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();
}
