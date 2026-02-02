namespace Propentro.Contracts.Organizations;

public record OrganizationDto(
    string PublicId,
    string Slug,
    string Name,
    string? ContactName,
    string? ContactEmail,
    string CurrencyCode,
    string Timezone,
    string SubscriptionPlan,
    DateTime CreatedAt);
