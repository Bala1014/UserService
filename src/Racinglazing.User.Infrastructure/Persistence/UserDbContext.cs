using Microsoft.EntityFrameworkCore;
using Racinglazing.User.Application.Abstractions.Persistence;
using Racinglazing.User.Domain.Entities;
using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Infrastructure.Persistence;

/// <summary>
/// The identity store. Doubles as the unit of work for the application layer —
/// repositories stage changes on its change tracker and the use case commits
/// them with a single <see cref="SaveChangesAsync"/>.
/// </summary>
public sealed class UserDbContext(DbContextOptions<UserDbContext> options)
    : DbContext(options), IUnitOfWork
{
    public DbSet<DomainUser> Users => Set<DomainUser>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
