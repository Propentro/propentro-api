using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Application.Features.Organizations.Roles.Queries.GetOrganizationRoles;

public sealed record GetOrganizationRolesQuery : ICachedQuery<IReadOnlyList<RoleResponse>>
{
    public string CacheKey => "roles";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
