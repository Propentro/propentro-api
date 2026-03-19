using Dapper;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Application.Features.Organizations.Roles.Queries.GetRole;

public sealed class GetRoleHandler(
    IDbConnectionFactory db,
    IUserContext currentUser) : IQueryHandler<GetRoleQuery, RoleResponse>
{
    public async Task<Result<RoleResponse>> Handle(
        GetRoleQuery request, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string roleSql = """
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
            WHERE r.public_id = @Id
              AND (r.organization_id = @OrganizationId OR r.is_system = true)
              AND r.is_active = true
            GROUP BY r.id, r.public_id, r.name, r.description, r.is_system
            """;

        var role = await connection.QueryFirstOrDefaultAsync<RoleQueryResult>(
            roleSql, new { currentUser.OrganizationId, request.Id });

        if (role is null)
            return RoleErrors.NotFound;

        const string permissionsSql = """
            SELECT p.code AS permission_code
            FROM role_permissions rp
            JOIN permissions p ON p.id = rp.permission_id
            WHERE rp.role_id = @RoleInternalId
            ORDER BY p.code
            """;

        var permissions = (await connection.QueryAsync<string>(
            permissionsSql, new { role.RoleInternalId })).ToList();

        return new RoleResponse
        {
            RoleId = role.RoleId,
            Name = role.Name,
            Description = role.Description,
            IsSystem = role.IsSystem,
            MemberCount = role.MemberCount,
            PermissionCodes = permissions
        };
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
}
