namespace Propentro.Contracts.Documents;

public sealed record InitiateContactDocumentUploadRequest(
    string Name,
    string DocumentType,
    string? Description,
    bool IsPrimary,
    string FileName,
    string ContentType,
    long FileSize);
