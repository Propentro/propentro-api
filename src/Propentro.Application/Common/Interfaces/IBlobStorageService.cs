namespace Propentro.Application.Common.Interfaces;

public interface IBlobStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    Task<Stream?> DownloadAsync(
        string blobName,
        CancellationToken ct = default);

    Task<bool> DeleteAsync(
        string blobName,
        CancellationToken ct = default);

    Task<string> GetPresignedUrlAsync(
        string blobName,
        TimeSpan expiry,
        CancellationToken ct = default);
}
