namespace Propentro.Contracts.Organizations;

public sealed record OrganizationSettingsResponse(
    string OrganizationId,
    string Name,
    string Slug,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? LegalName,
    string? TaxNumber,
    string? TaxCountry,
    string CurrencyCode,
    string Timezone);
