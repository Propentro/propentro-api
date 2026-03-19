using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    string Id,
    string Name,
    string Slug,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string CurrencyCode,
    string Timezone,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? Region,
    string? PostalCode,
    string? CountryCode,
    string? LegalName,
    string? TaxNumber,
    string? TaxCountry) : ICommand<AdminOrganizationResponse>;
