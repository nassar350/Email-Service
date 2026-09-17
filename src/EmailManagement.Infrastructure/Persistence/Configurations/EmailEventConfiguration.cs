using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class EmailEventConfiguration : IEntityTypeConfiguration<EmailEvent>
{
    public void Configure(EntityTypeBuilder<EmailEvent> builder)
    {
        builder.HasKey(x => x.EventId);
        builder.Property(x => x.EventId).ValueGeneratedNever();
        builder.Property(x => x.EventType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.EventData);
        builder.Property(x => x.ProviderEventId).HasMaxLength(200);
        builder.Property(x => x.OccurredAt).IsRequired();

        builder.HasIndex(x => x.EmailId);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.OccurredAt);
        builder.HasIndex(x => x.ProviderEventId).IsUnique().HasFilter(null);
        builder.HasIndex(x => x.RecipientId);
    }
}