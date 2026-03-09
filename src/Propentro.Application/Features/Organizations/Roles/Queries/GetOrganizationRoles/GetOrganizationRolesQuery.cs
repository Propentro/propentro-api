using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Application.Features.Organizations.Roles.Queries.GetOrganizationRoles;

public sealed record GetOrganizationRolesQuery : IQuery<IReadOnlyList<RoleResponse>>;
