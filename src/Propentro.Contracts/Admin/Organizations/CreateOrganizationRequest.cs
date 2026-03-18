namespace Propentro.Contracts.Admin.Organizations;

public sealed record CreateOrganizationRequest(
    string Name,
    string Slug,
    string CurrencyCode,
    string Timezone);
