using Microsoft.Extensions.Configuration;
using Propentro.Application.Common.Interfaces;
using Serilog;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Blob storage service for Cloudflare R2 (S3-compatible API).
/// Configure R2 credentials in appsettings.json under "BlobStorage" section.
/// </summary>
public sealed class BlobStorageService(IConfiguration configuration) : IBlobStorageService
{
    private readonly string _bucketName = configuration["BlobStorage:BucketName"] ?? "propentro";
    private readonly string _accountId = configuration["BlobStorage:AccountId"] ?? "";
    private readonly string _accessKeyId = configuration["BlobStorage:AccessKeyId"] ?? "";
    private readonly string _secretAccessKey = configuration["BlobStorage:SecretAccessKey"] ?? "";

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default)
    {
        var blobName = $"{Guid.NewGuid():N}/{fileName}";

        Log.Information(
            "Uploading blob {BlobName} to bucket {BucketName}",
            blobName,
            _bucketName);

        // TODO: Implement R2 upload using S3-compatible SDK
        // For now, return the blob name that would be used
        await Task.CompletedTask;

        return blobName;
    }

    public async Task<Stream?> DownloadAsync(
        string blobName,
        CancellationToken ct = default)
    {
        Log.Information(
            "Downloading blob {BlobName} from bucket {BucketName}",
            blobName,
            _bucketName);

        // TODO: Implement R2 download using S3-compatible SDK
        await Task.CompletedTask;

        return null;
    }

    public async Task<bool> DeleteAsync(
        string blobName,
        CancellationToken ct = default)
    {
        Log.Information(
            "Deleting blob {BlobName} from bucket {BucketName}",
            blobName,
            _bucketName);

        // TODO: Implement R2 delete using S3-compatible SDK
        await Task.CompletedTask;

        return true;
    }

    public async Task<string> GetPresignedUrlAsync(
        string blobName,
        TimeSpan expiry,
        CancellationToken ct = default)
    {
        Log.Information(
            "Generating presigned URL for blob {BlobName} with expiry {Expiry}",
            blobName,
            expiry);

        // TODO: Implement R2 presigned URL generation
        await Task.CompletedTask;

        // Return placeholder URL
        return $"https://{_accountId}.r2.cloudflarestorage.com/{_bucketName}/{blobName}";
    }
}
