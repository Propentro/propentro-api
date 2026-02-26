namespace Propentro.Contracts.Documents;

public sealed record ContactDocumentResponse(
    string DocumentId,
    string Name,
    string DocumentType,
    string? Description,
    bool IsPrimary,
    string OriginalFileName,
    string MimeType,
    long? FileSize,
    string? DownloadUrl,
    DateTimeOffset CreatedAt);
