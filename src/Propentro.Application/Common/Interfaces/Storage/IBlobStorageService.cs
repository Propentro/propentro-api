namespace Propentro.Application.Common.Interfaces.Storage;

public interface IBlobStorageService
{
    Task UploadAsync(
        string storageKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default);

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
