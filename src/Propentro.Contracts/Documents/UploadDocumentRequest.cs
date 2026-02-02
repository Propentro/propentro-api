namespace Propentro.Contracts.Documents;

public record UploadDocumentRequest(
    string OriginalName,
    string MimeType,
    string DocumentType,
    string? Description);

public record UploadDocumentResponse(
    string PublicId,
    string UploadUrl,
    DateTime ExpiresAt);
