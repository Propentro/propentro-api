namespace Propentro.Contracts.Contacts;

public sealed record ContactResponse(
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
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Trade,
    string? TradeOther,
    string? TaxNumber,
    string? TaxCountry,
    string? Note,
    bool IsArchived,
    DateTimeOffset? ArchivedAt,
    IReadOnlyList<string> Roles,
    ContactAddressResponse? Address,
    string? CreatedByName,
    string? UpdatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

public sealed record ContactAddressResponse(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode);
