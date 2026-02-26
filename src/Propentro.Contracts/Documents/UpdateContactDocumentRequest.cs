namespace Propentro.Contracts.Documents;

public sealed record UpdateContactDocumentRequest(
    string? Name,
    string? Description,
    bool? IsPrimary);
