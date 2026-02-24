using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Serilog;

namespace Propentro.Infrastructure.Storage;

/// <summary>
/// Blob storage service for Cloudflare R2 (S3-compatible API).
/// Configure R2 credentials in appsettings.json under "BlobStorage" section.
/// </summary>
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

    public async Task<string> UploadAsync(
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

        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = blobName,
            InputStream = content,
            ContentType = contentType
        };

        await _s3.PutObjectAsync(request, cancellationToken);

        return blobName;
    }

    public async Task<Stream?> DownloadAsync(string blobName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Information("Downloading blob {BlobName} from bucket {BucketName}", blobName, _bucketName);

        try
        {
            var response = await _s3.GetObjectAsync(_bucketName, blobName, cancellationToken);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound)
        {
            Log.Warning("Blob {BlobName} not found in bucket {BucketName}", blobName, _bucketName);
            return null;
        }
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

    public async Task<string> GetPresignedUrlAsync(
        string blobName,
        TimeSpan expiry,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(blobName);

        Log.Information("Generating presigned URL for blob {BlobName} with expiry {Expiry}", blobName, expiry);

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = blobName,
            Expires = DateTime.UtcNow.Add(expiry),
            Verb = HttpVerb.GET
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
