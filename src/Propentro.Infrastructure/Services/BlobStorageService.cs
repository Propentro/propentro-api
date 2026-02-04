using Microsoft.Extensions.Configuration;
using Serilog;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Blob storage service for Cloudflare R2 (S3-compatible API).
/// Configure R2 credentials in appsettings.json under "BlobStorage" section.
/// </summary>
public sealed class BlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private readonly string _bucketName = configuration["BlobStorage:BucketName"] ?? "propentro";
    private readonly string _accountId = configuration["BlobStorage:AccountId"] ?? string.Empty;

    public Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var blobName = $"{Guid.NewGuid():N}/{fileName}";

        Log.Information(
            "Uploading blob {BlobName} to bucket {BucketName} with content type {ContentType}",
            blobName, _bucketName, contentType);

        // TODO: Implement R2 upload using AWS SDK for .NET (S3-compatible)
        return Task.FromResult(blobName);
    }

    public Task<Stream?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Information("Downloading blob {BlobName} from bucket {BucketName}", blobName, _bucketName);

        // TODO: Implement R2 download using AWS SDK for .NET
        return Task.FromResult<Stream?>(null);
    }

    public Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Information("Deleting blob {BlobName} from bucket {BucketName}", blobName, _bucketName);

        // TODO: Implement R2 delete using AWS SDK for .NET
        return Task.FromResult(true);
    }

    public Task<string> GetPresignedUrlAsync(
        string blobName,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Information("Generating presigned URL for blob {BlobName} with expiry {Expiry}", blobName, expiry);

        // TODO: Implement R2 presigned URL generation
        return Task.FromResult($"https://{_accountId}.r2.cloudflarestorage.com/{_bucketName}/{blobName}");
    }

    public Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Debug("Checking existence of blob {BlobName} in bucket {BucketName}", blobName, _bucketName);

        // TODO: Implement R2 existence check
        return Task.FromResult(false);
    }
}
