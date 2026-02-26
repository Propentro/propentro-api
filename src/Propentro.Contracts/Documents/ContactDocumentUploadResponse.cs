namespace Propentro.Contracts.Documents;

public sealed record ContactDocumentUploadResponse(
    string DocumentId,
    string UploadUrl);
