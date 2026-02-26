using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Serilog;

namespace Propentro.Infrastructure.Storage;

public sealed class BlobStorageService : IBlobStorageService, IDisposable
{
    private readonly AmazonS3Client _s3;
    private readonly string _bucketName;

    public BlobStorageService(IOptions<BlobStorageOptions> options)
    {
        var config = options.Value;
        _bucketName = config.BucketName;

        _s3 = new AmazonS3Client(
            config.AccessKeyId,
            config.SecretAccessKey,
            new AmazonS3Config
            {
                ServiceURL = config.Endpoint,
                ForcePathStyle = true
            });
    }

    public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Information("Deleting blob {BlobName} from bucket {BucketName}", blobName, _bucketName);

        try
        {
            await _s3.DeleteObjectAsync(_bucketName, blobName, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            Log.Warning("Blob {BlobName} not found for deletion in bucket {BucketName}", blobName, _bucketName);
            return false;
        }
    }

    public async Task<string> GetPresignedDownloadUrlAsync(
        string storageKey,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        Log.Information("Generating presigned download URL for blob {StorageKey} with expiry {Expiry}", storageKey, expiry);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
        };

        string url = await _s3.GetPreSignedURLAsync(request);

        return url;
    }

    public async Task<string> GetPresignedUploadUrlAsync(
        string storageKey,
        string contentType,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        Log.Information("Generating presigned upload URL for blob {StorageKey} with expiry {Expiry}", storageKey, expiry);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = storageKey,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.PUT,
            ContentType = contentType
        };

        string url = await _s3.GetPreSignedURLAsync(request);

        return url;
    }

    public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Debug("Checking existence of blob {BlobName} in bucket {BucketName}", blobName, _bucketName);

        try
        {
            await _s3.GetObjectMetadataAsync(_bucketName, blobName, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public void Dispose() => _s3.Dispose();
}
