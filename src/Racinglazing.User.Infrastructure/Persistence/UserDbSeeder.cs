using Microsoft.EntityFrameworkCore;
using Racinglazing.User.Application.Common;
using Racinglazing.User.Domain.Entities;

namespace Racinglazing.User.Infrastructure.Persistence;

/// <summary>
/// Seeds the roles the platform relies on. The baseline <c>User</c> role must
/// exist before anyone can register (it is granted on sign-up), so this runs on
/// startup in every environment — it is idempotent, only inserting roles that
/// are missing.
/// </summary>
public static class UserDbSeeder
{
    public static async Task SeedAsync(UserDbContext db, CancellationToken ct = default)
    {
        var existing = await db.Roles.Select(r => r.NormalizedName).ToListAsync(ct);
        var existingSet = existing.ToHashSet();

        var toAdd = DefaultRoles.All
            .Where(r => !existingSet.Contains(r.Name.ToUpperInvariant()))
            .Select(r => new Role(r.Name, r.Description))
            .ToList();

        if (toAdd.Count == 0) return;

        db.Roles.AddRange(toAdd);
        await db.SaveChangesAsync(ct);
    }
}
