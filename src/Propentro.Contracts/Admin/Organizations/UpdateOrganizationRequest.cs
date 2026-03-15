namespace Propentro.Contracts.Admin.Organizations;

public sealed record UpdateOrganizationRequest(
    string Name,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string CurrencyCode,
    string Timezone);
