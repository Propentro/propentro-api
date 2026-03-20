using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Application.Features.Admin.Organizations.Queries.GetOrganizationRoles;

public sealed record GetOrganizationRolesQuery(string OrganizationPublicId)
    : IQuery<List<AdminOrgRoleResponse>>;
