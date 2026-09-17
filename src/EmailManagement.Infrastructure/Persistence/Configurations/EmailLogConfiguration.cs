using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.HasKey(x => x.LogId);
        builder.Property(x => x.LogId).ValueGeneratedNever();
        builder.Property(x => x.Level).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.Details);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.EmailId);
        builder.HasIndex(x => x.CreatedAt);
    }
}