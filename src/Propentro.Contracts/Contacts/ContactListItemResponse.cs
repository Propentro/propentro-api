namespace Propentro.Contracts.Contacts;

public sealed record ContactListItemResponse(
    string ContactId,
    string Kind,
    string DisplayName,
    string? FirstName,
    string? LastName,
    string? LegalName,
    string? TradingName,
    string? Email,
    string? Phone,
    string? SecondaryEmail,
    string? SecondaryPhone,
    string? Trade,
    bool IsArchived,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);
