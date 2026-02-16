namespace Propentro.Contracts.Contacts;

public sealed record ContactListItemResponse(
    string Id,
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
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? TradeOther,
    string? TaxNumber,
    string? TaxCountry,
    bool IsArchived,
    IReadOnlyList<string> Roles,
    DateTimeOffset CreatedAt);
