using Propentro.Contracts.Organizations.Roles.Permissions;

namespace Propentro.Application.Features.Organizations.Roles.Permissions.Queries.GetPermissions;

public sealed record GetPermissionsQuery : ICachedQuery<IReadOnlyList<PermissionGroupResponse>>
{
    public string CacheKey => "permissions";
    public TimeSpan? Expiration => TimeSpan.FromHours(1);
}
