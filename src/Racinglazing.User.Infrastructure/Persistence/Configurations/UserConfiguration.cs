using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Racinglazing.User.Domain.Entities;
using DomainUser = Racinglazing.User.Domain.Entities.User;

namespace Racinglazing.User.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the <see cref="DomainUser"/> aggregate. Uniqueness is enforced on the
/// normalised columns (case-insensitive), and the backing fields for the
/// aggregate's collections are mapped so the entity stays encapsulated.
/// </summary>
public sealed class UserConfiguration : IEntityTypeConfiguration<DomainUser>
{
    public void Configure(EntityTypeBuilder<DomainUser> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).IsRequired().HasMaxLength(256);
        builder.Property(u => u.EmailVerified).HasDefaultValue(false);

        builder.Property(u => u.Username).IsRequired().HasMaxLength(32);
        builder.Property(u => u.NormalizedUsername).IsRequired().HasMaxLength(32);

        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(80);
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.Bio).HasMaxLength(1000);

        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.Property(u => u.SecurityStamp).IsRequired().HasMaxLength(64);

        builder.Property(u => u.Status).HasConversion<int>();

        builder.Property(u => u.CreatedAt);
        builder.Property(u => u.UpdatedAt);
        builder.Property(u => u.LastLoginAt);

        builder.HasIndex(u => u.NormalizedEmail).IsUnique().HasDatabaseName("ix_users_normalized_email");
        builder.HasIndex(u => u.NormalizedUsername).IsUnique().HasDatabaseName("ix_users_normalized_username");

        // Map the aggregate's private collection backing fields.
        builder.Metadata
            .FindNavigation(nameof(DomainUser.Roles))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata
            .FindNavigation(nameof(DomainUser.ExternalLogins))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
        builder.Metadata
            .FindNavigation(nameof(DomainUser.RefreshTokens))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.Roles).WithOne().HasForeignKey(ur => ur.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.ExternalLogins).WithOne().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(u => u.RefreshTokens).WithOne(t => t.User!).HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
