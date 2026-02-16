namespace Propentro.Contracts.Contacts;

public sealed record UpdateContactRequest(
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
    UpdateContactAddressRequest? Address);

public sealed record UpdateContactAddressRequest(
    string? Line1,
    string? Line2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode);
