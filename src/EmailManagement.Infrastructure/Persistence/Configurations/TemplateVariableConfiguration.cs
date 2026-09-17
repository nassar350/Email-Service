using EmailManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EmailManagement.Infrastructure.Persistence.Configurations;

public class TemplateVariableConfiguration : IEntityTypeConfiguration<TemplateVariable>
{
    public void Configure(EntityTypeBuilder<TemplateVariable> builder)
    {
        builder.HasKey(x => x.VariableId);
        builder.Property(x => x.VariableId).ValueGeneratedNever();
        builder.Property(x => x.VariableKey).IsRequired().HasMaxLength(200);
        builder.Property(x => x.DisplayName).HasMaxLength(200);
        builder.Property(x => x.DataType).IsRequired().HasMaxLength(50).HasDefaultValue("string");
        builder.Property(x => x.DefaultValue).HasMaxLength(2000);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasIndex(x => new { x.TemplateId, x.VariableKey }).IsUnique();
    }
}