using Propentro.Contracts.Tags;

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
    string? TaxCountryName,
    string? Note,
    bool IsArchived,
    DateTimeOffset? ArchivedAt,
    string? ArchivedByName,
    string? UserId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<TagResponse> Tags,
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
    string CountryName);
