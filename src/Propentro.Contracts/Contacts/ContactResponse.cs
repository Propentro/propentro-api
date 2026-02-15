namespace Propentro.Contracts.Contacts;

public sealed record ContactResponse(
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
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Trade,
    string? TradeOther,
    string? TaxNumber,
    string? TaxCountry,
    string? Note,
    bool IsArchived,
    IReadOnlyList<string> Roles,
    ContactAddressResponse? Address,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

public sealed record ContactAddressResponse(
    string Line1,
    string? Line2,
    string City,
    string? Region,
    string? PostalCode,
    string CountryCode);
