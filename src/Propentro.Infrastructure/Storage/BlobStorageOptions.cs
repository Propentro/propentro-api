namespace Propentro.Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string BucketName { get; init; } = string.Empty;

    public string Endpoint { get; init; } = string.Empty;

    public string AccessKeyId { get; init; } = string.Empty;

    public string SecretAccessKey { get; init; } = string.Empty;
}
