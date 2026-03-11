namespace Propentro.Application.Common.Interfaces.Storage;

public interface IBlobStorageService
{
    Task<bool> DeleteAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<string> GetPresignedUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default);
}
