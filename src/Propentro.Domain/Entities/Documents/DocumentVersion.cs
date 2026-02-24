using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Documents;

public sealed class DocumentVersion : Entity
{
    public const string PublicIdPrefix = "docv";

    public Guid DocumentId { get; set; }
    public string OriginalName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public long? FileSize { get; set; }
    public string StorageKey { get; set; } = default!;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string Status { get; set; } = "pending";
    public Guid? UploadedById { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // Navigation
    public Document Document { get; set; } = default!;
}
