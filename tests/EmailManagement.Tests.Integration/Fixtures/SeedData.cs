using EmailManagement.Domain.Entities;
using EmailManagement.Domain.Enums;
using EmailManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EmailManagement.Tests.Integration.Fixtures;

public static class SeedData
{
    public static async Task<EmailTemplate> CreateTemplateAsync(
        ApplicationDbContext db,
        string key = "welcome",
        TemplateStatus status = TemplateStatus.Active)
    {
        var now = DateTime.UtcNow;
        var template = new EmailTemplate
        {
            TemplateId = Guid.NewGuid(),
            TemplateKey = key,
            Name = "Welcome",
            Description = null,
            SubjectTemplate = "Hi {{name}}",
            HtmlBody = "<h1>Hello {{name}}</h1>",
            TextBody = "Hello {{name}}",
            Status = status,
            CurrentVersion = 1,
            CreatedAt = now,
            UpdatedAt = now,
        };

        template.Variables.Add(new TemplateVariable
        {
            VariableId = Guid.NewGuid(),
            TemplateId = template.TemplateId,
            VariableKey = "name",
            DisplayName = "Name",
            IsRequired = true,
            CreatedAt = now,
        });

        template.Versions.Add(new TemplateVersion
        {
            VersionId = Guid.NewGuid(),
            TemplateId = template.TemplateId,
            VersionNumber = 1,
            SubjectTemplate = template.SubjectTemplate,
            HtmlBody = template.HtmlBody,
            TextBody = template.TextBody,
            ChangeDescription = "Initial",
            IsActive = true,
            CreatedAt = now,
        });

        db.EmailTemplates.Add(template);
        await db.SaveChangesAsync();
        return template;
    }

    public static async Task<EmailProvider> CreateProviderAsync(
        ApplicationDbContext db,
        ProviderType type = ProviderType.Mailjet,
        bool isDefault = true)
    {
        var provider = new EmailProvider
        {
            ProviderId = Guid.NewGuid(),
            ProviderType = type,
            ProviderName = "Test Provider",
            Configuration = "{\"fromEmail\":\"noreply@example.com\"}",
            IsDefault = isDefault,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        db.EmailProviders.Add(provider);
        await db.SaveChangesAsync();
        return provider;
    }
}