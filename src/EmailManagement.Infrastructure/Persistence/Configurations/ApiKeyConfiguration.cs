using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(x => x.ApiKeyId);
        builder.Property(x => x.ApiKeyId).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(200);
        builder.Property(x => x.KeyPrefix).IsRequired().HasMaxLength(50);
        builder.Property(x => x.KeyHash).IsRequired().HasMaxLength(200);
        builder.HasIndex(x => x.KeyHash).IsUnique();
        builder.HasIndex(x => x.KeyPrefix);
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.LastUsedAt);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.RevokedAt);
    }
}