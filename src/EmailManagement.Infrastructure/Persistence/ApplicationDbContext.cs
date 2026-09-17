using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
    public DbSet<TemplateVersion> TemplateVersions => Set<TemplateVersion>();
    public DbSet<TemplateVariable> TemplateVariables => Set<TemplateVariable>();
    public DbSet<EmailProvider> EmailProviders => Set<EmailProvider>();
    public DbSet<Email> Emails => Set<Email>();
    public DbSet<EmailRecipient> EmailRecipients => Set<EmailRecipient>();
    public DbSet<EmailAttachment> EmailAttachments => Set<EmailAttachment>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}