using EmailManagement.Domain.Enums;

namespace EmailManagement.Domain.Entities;

public class EmailAttachment
{
    public Guid AttachmentId { get; set; }
    public Guid EmailId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public AttachmentStorageType StorageType { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Email EmailEntity { get; set; } = null!;
}