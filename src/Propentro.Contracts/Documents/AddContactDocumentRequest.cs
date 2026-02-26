namespace Propentro.Contracts.Documents;

public sealed record AddContactDocumentRequest(
    string Name,
    string DocumentType,
    string? Description,
    bool IsPrimary);
