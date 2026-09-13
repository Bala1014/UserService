using Microsoft.EntityFrameworkCore;
using Racinglazing.User.Application.Abstractions.Persistence;
using Racinglazing.User.Domain.Entities;

namespace Racinglazing.User.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository(UserDbContext db) : IRoleRepository
{
    public Task<Role?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var normalized = name.Trim().ToUpperInvariant();
        return db.Roles.FirstOrDefaultAsync(r => r.NormalizedName == normalized, ct);
    }

    public async Task<IReadOnlyList<Role>> GetByNamesAsync(IReadOnlyCollection<string> names, CancellationToken ct = default)
    {
        if (names.Count == 0) return [];
        var normalized = names.Select(n => n.Trim().ToUpperInvariant()).ToArray();
        return await db.Roles.Where(r => normalized.Contains(r.NormalizedName)).ToListAsync(ct);
    }
}
