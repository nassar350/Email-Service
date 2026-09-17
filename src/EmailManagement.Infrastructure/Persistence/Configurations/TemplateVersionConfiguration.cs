using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class TemplateVersionConfiguration : IEntityTypeConfiguration<TemplateVersion>
{
    public void Configure(EntityTypeBuilder<TemplateVersion> builder)
    {
        builder.HasKey(x => x.VersionId);
        builder.Property(x => x.VersionId).ValueGeneratedNever();
        builder.Property(x => x.VersionNumber).IsRequired();
        builder.Property(x => x.SubjectTemplate).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.HtmlBody).IsRequired();
        builder.Property(x => x.TextBody);
        builder.Property(x => x.ChangeDescription).HasMaxLength(2000);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200);

        builder.HasIndex(x => new { x.TemplateId, x.VersionNumber }).IsUnique();
        builder.HasIndex(x => x.IsActive);

        builder.HasMany(x => x.Emails)
            .WithOne(x => x.Version)
            .HasForeignKey(x => x.VersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}