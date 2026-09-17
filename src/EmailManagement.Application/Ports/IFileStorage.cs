using EmailManagement.Domain.Entities;

namespace EmailManagement.Application.Ports;

public interface IFileStorage
{
    Task<FileStorageResult> SaveAsync(Stream content, string fileName, string contentType, CancellationToken ct = default);
    Task<Stream?> GetAsync(string storagePath, CancellationToken ct = default);
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}

public record FileStorageResult(string StorageType, string StoragePath, long FileSize);