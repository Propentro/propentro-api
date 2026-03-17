using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Application.Features.Organizations.Roles.Queries.GetOrganizationRoles;

public sealed record GetOrganizationRolesQuery(
    int Page = 1,
    int PageSize = 25) : ICachedQuery<PagedResult<RoleResponse>>
{
    public string CacheKey => $"roles-{Page}-{PageSize}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
