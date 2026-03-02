using Propentro.Contracts.Organizations;

namespace Propentro.Application.Features.Organizations.Commands.UpdateOrganizationSettings;

public sealed record UpdateOrganizationSettingsCommand(
    string Name,
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
    string Timezone) : ICommand<OrganizationSettingsResponse>;
