using Propentro.Domain.Common;

namespace Propentro.Domain.Entities;

public sealed class Document : OrganizationEntity
{
    public string OriginalName { get; set; } = default!;
    public string MimeType { get; set; } = default!;
    public long? FileSize { get; set; }
    public string StorageKey { get; set; } = default!;
    public string? Description { get; set; }
    public string DocumentType { get; set; } = default!;
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string Status { get; set; } = "pending";
    public Guid? UploadedById { get; set; }
}
