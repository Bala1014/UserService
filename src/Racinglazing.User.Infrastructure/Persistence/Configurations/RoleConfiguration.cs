using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Racinglazing.User.Domain.Entities;

namespace Racinglazing.User.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).ValueGeneratedNever();
        builder.Property(r => r.Name).IsRequired().HasMaxLength(64);
        builder.Property(r => r.NormalizedName).IsRequired().HasMaxLength(64);
        builder.Property(r => r.Description).HasMaxLength(256);

        builder.HasIndex(r => r.NormalizedName).IsUnique().HasDatabaseName("ix_roles_normalized_name");
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.HasOne(ur => ur.Role)
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ur => ur.RoleId).HasDatabaseName("ix_user_roles_role_id");
    }
}
