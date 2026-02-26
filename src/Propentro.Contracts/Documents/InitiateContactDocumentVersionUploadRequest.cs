namespace Propentro.Contracts.Documents;

public sealed record InitiateContactDocumentVersionUploadRequest(
    string FileName,
    string ContentType,
    long FileSize);
