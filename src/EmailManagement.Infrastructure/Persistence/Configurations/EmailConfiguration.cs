using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class EmailConfiguration : IEntityTypeConfiguration<Email>
{
    public void Configure(EntityTypeBuilder<Email> builder)
    {
        builder.HasKey(x => x.EmailId);
        builder.Property(x => x.EmailId).ValueGeneratedNever();
        builder.Property(x => x.MessageId).HasMaxLength(100);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(50);
        builder.Property(x => x.Subject).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.FromEmail).IsRequired().HasMaxLength(320);
        builder.Property(x => x.FromName).HasMaxLength(200);
        builder.Property(x => x.ReplyTo).HasMaxLength(320);
        builder.Property(x => x.TemplateData);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.Property(x => x.MaxRetries).HasDefaultValue(3);
        builder.Property(x => x.NextRetryAt);
        builder.Property(x => x.LastError).HasMaxLength(2000);
        builder.Property(x => x.QueuedAt).IsRequired();
        builder.Property(x => x.ProcessingAt);
        builder.Property(x => x.SentAt);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.QueuedAt);
        builder.HasIndex(x => x.NextRetryAt);
        builder.HasIndex(x => x.MessageId);
        builder.HasIndex(x => x.TemplateId);
        builder.HasIndex(x => x.VersionId);
        builder.HasIndex(x => x.ProviderId);
        builder.HasIndex(x => x.CreatedAt);

        builder.HasMany(x => x.Recipients)
            .WithOne(x => x.EmailEntity)
            .HasForeignKey(x => x.EmailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Attachments)
            .WithOne(x => x.EmailEntity)
            .HasForeignKey(x => x.EmailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Events)
            .WithOne(x => x.EmailEntity)
            .HasForeignKey(x => x.EmailId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Logs)
            .WithOne(x => x.EmailEntity)
            .HasForeignKey(x => x.EmailId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}