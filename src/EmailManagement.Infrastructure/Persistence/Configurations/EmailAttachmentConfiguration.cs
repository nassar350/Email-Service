using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class EmailAttachmentConfiguration : IEntityTypeConfiguration<EmailAttachment>
{
    public void Configure(EntityTypeBuilder<EmailAttachment> builder)
    {
        builder.HasKey(x => x.AttachmentId);
        builder.Property(x => x.AttachmentId).ValueGeneratedNever();
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(500);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FileSize).IsRequired();
        builder.Property(x => x.StorageType).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.StoragePath).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.EmailId);
    }
}