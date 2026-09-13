using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Racinglazing.User.Domain.Entities;

namespace Racinglazing.User.Infrastructure.Persistence.Configurations;

public sealed class ExternalLoginConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
    {
        builder.ToTable("external_logins");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).ValueGeneratedNever();
        builder.Property(e => e.Provider).IsRequired().HasMaxLength(32);
        builder.Property(e => e.ProviderKey).IsRequired().HasMaxLength(256);
        builder.Property(e => e.CreatedAt);

        // One external identity maps to at most one local account.
        builder.HasIndex(e => new { e.Provider, e.ProviderKey })
            .IsUnique()
            .HasDatabaseName("ix_external_logins_provider_provider_key");

        builder.HasIndex(e => e.UserId).HasDatabaseName("ix_external_logins_user_id");
    }
}

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).ValueGeneratedNever();
        builder.Property(t => t.TokenHash).IsRequired().HasMaxLength(128);
        builder.Property(t => t.ExpiresAt);
        builder.Property(t => t.CreatedAt);
        builder.Property(t => t.CreatedByIp).HasMaxLength(64);
        builder.Property(t => t.RevokedAt);
        builder.Property(t => t.RevokedByIp).HasMaxLength(64);
        builder.Property(t => t.ReplacedByTokenHash).HasMaxLength(128);

        builder.HasIndex(t => t.TokenHash).IsUnique().HasDatabaseName("ix_refresh_tokens_token_hash");
        builder.HasIndex(t => t.UserId).HasDatabaseName("ix_refresh_tokens_user_id");
    }
}
