namespace Propentro.Contracts.Contacts;

public sealed record ContactListItemResponse(
    string ContactId,
    string Kind,
    string DisplayName,
    string? Email,
    string? Phone,
    string? Trade,
    bool IsArchived,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt);
