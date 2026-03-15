namespace Propentro.Contracts.Admin.Organizations;

public sealed record CreateOrganizationRequest(
    string Name,
    string Slug,
    string? ContactName,
    string? ContactEmail,
    string CurrencyCode,
    string Timezone,
    string SubscriptionPlan);
