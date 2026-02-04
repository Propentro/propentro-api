namespace Propentro.Application.Common.Interfaces.Storage;

/// <summary>
/// Abstraction for blob storage operations.
/// Supports S3-compatible storage providers (AWS S3, Cloudflare R2, MinIO, etc.).
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to blob storage.
    /// </summary>
    /// <param name="content">The file content stream.</param>
    /// <param name="fileName">The original file name.</param>
    /// <param name="contentType">The MIME content type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The unique blob name/path for retrieval.</returns>
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a file from blob storage.
    /// </summary>
    /// <param name="blobName">The blob name/path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content stream, or null if not found.</returns>
    Task<Stream?> DownloadAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from blob storage.
    /// </summary>
    /// <param name="blobName">The blob name/path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if deleted successfully, false if not found.</returns>
    Task<bool> DeleteAsync(
        string blobName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a presigned URL for temporary direct access to a blob.
    /// </summary>
    /// <param name="blobName">The blob name/path.</param>
    /// <param name="expiry">How long the URL should be valid.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A presigned URL for direct access.</returns>
    Task<string> GetPresignedUrlAsync(
        string blobName,
        TimeSpan expiry,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a blob exists in storage.
    /// </summary>
    /// <param name="blobName">The blob name/path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the blob exists, false otherwise.</returns>
    Task<bool> ExistsAsync(
        string blobName,
        CancellationToken cancellationToken = default);
}
