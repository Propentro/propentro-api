namespace Propentro.Contracts.Documents;

public record DocumentDto(
    string PublicId,
    string OriginalName,
    string MimeType,
    long? FileSize,
    string DocumentType,
    string Status,
    string? Description,
    DateTime CreatedAt);
