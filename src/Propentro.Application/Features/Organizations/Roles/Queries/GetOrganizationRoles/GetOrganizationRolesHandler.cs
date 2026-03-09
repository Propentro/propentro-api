using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Application.Features.Organizations.Roles.Queries.GetOrganizationRoles;

public sealed class GetOrganizationRolesHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetOrganizationRolesQuery, IReadOnlyList<RoleResponse>>
{
    public async Task<Result<IReadOnlyList<RoleResponse>>> Handle(
        GetOrganizationRolesQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string rolesSql = """
            SELECT
                r.public_id AS role_id,
                r.id AS role_internal_id,
                r.name,
                r.description,
                r.is_system,
                COUNT(our.user_id) AS member_count
            FROM roles r
            LEFT JOIN organization_user_roles our
                ON our.role_id = r.id AND our.organization_id = @OrganizationId
            WHERE (r.organization_id = @OrganizationId OR r.is_system = true)
              AND r.is_active = true
            GROUP BY r.id, r.public_id, r.name, r.description, r.is_system
            ORDER BY r.is_system DESC, r.name
            """;

        var roles = (await connection.QueryAsync<RoleQueryResult>(
            rolesSql,
            new { currentUser.OrganizationId }))
            .ToList();

        if (roles.Count == 0)
            return Result.Success<IReadOnlyList<RoleResponse>>([]);

        var roleInternalIds = roles.Select(r => r.RoleInternalId).ToArray();

        const string permissionsSql = """
            SELECT
                rp.role_id AS role_internal_id,
                p.code AS permission_code
            FROM role_permissions rp
            JOIN permissions p ON p.id = rp.permission_id
            WHERE rp.role_id = ANY(@RoleIds)
            ORDER BY p.code
            """;

        var permissionRows = (await connection.QueryAsync<RolePermissionRow>(
            permissionsSql,
            new { RoleIds = roleInternalIds }))
            .ToList();

        var permissionsLookup = permissionRows
            .GroupBy(p => p.RoleInternalId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<string>)g.Select(p => p.PermissionCode).ToList());

        var result = roles.Select(r => new RoleResponse(
            r.RoleId,
            r.Name,
            r.Description,
            r.IsSystem,
            r.MemberCount,
            permissionsLookup.GetValueOrDefault(r.RoleInternalId, [])))
            .ToList();

        return result;
    }

    private sealed class RoleQueryResult
    {
        public string RoleId { get; init; } = null!;
        public Guid RoleInternalId { get; init; }
        public string Name { get; init; } = null!;
        public string? Description { get; init; }
        public bool IsSystem { get; init; }
        public int MemberCount { get; init; }
    }

    private sealed class RolePermissionRow
    {
        public Guid RoleInternalId { get; init; }
        public string PermissionCode { get; init; } = null!;
    }
}
