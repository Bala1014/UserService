using Microsoft.EntityFrameworkCore;
using Racinglazing.User.Application.Abstractions.Persistence;
using Racinglazing.User.Domain.Entities;

namespace Racinglazing.User.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(UserDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => db.RefreshTokens
            .Include(t => t.User!).ThenInclude(u => u.Roles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public async Task<IReadOnlyList<RefreshToken>> GetActiveForUserAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default)
        => await db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);

    public void Add(RefreshToken token) => db.RefreshTokens.Add(token);
}
