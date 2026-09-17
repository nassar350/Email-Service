using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class EmailProviderConfiguration : IEntityTypeConfiguration<EmailProvider>
{
    public void Configure(EntityTypeBuilder<EmailProvider> builder)
    {
        builder.HasKey(x => x.ProviderId);
        builder.Property(x => x.ProviderId).ValueGeneratedNever();
        builder.Property(x => x.ProviderType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.ProviderName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Configuration);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasMany(x => x.Emails)
            .WithOne(x => x.Provider)
            .HasForeignKey(x => x.ProviderId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}