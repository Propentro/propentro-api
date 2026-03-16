using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Commands.UpdateOrganization;

public sealed record UpdateOrganizationCommand(
    string Id,
    string Name,
    string? ContactName,
    string? ContactEmail,
    string? ContactPhone,
    string CurrencyCode,
    string Timezone) : ICommand<AdminOrganizationResponse>;
