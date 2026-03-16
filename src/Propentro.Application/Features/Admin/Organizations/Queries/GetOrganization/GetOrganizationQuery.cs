using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Queries.GetOrganization;

public sealed record GetOrganizationQuery(string Id) : IQuery<AdminOrganizationResponse>;
