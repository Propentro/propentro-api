using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Commands.CreateOrganization;

public sealed record CreateOrganizationCommand(
    string Name,
    string Slug,
    string? ContactName,
    string? ContactEmail,
    string CurrencyCode,
    string Timezone,
    string SubscriptionPlan) : ICommand<AdminOrganizationResponse>;
