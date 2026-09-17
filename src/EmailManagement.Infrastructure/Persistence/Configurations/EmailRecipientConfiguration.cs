using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class EmailRecipientConfiguration : IEntityTypeConfiguration<EmailRecipient>
{
    public void Configure(EntityTypeBuilder<EmailRecipient> builder)
    {
        builder.HasKey(x => x.RecipientId);
        builder.Property(x => x.RecipientId).ValueGeneratedNever();
        builder.Property(x => x.Email).IsRequired().HasMaxLength(320);
        builder.Property(x => x.RecipientType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.DeliveredAt);
        builder.Property(x => x.OpenedAt);
        builder.Property(x => x.ClickedAt);

        builder.HasIndex(x => x.EmailId);
        builder.HasIndex(x => x.Email);

        builder.HasMany(x => x.Events)
            .WithOne(x => x.Recipient)
            .HasForeignKey(x => x.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}