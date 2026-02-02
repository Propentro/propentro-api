using Dapper;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Services;

public sealed class PermissionService(IDbConnectionFactory db) : IPermissionService
{
    public async Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        using var connection = db.CreateConnection();

        const string sql = """
            SELECT DISTINCT p.code
            FROM permissions p
            INNER JOIN role_permissions rp ON rp.permission_id = p.id
            INNER JOIN organization_user_roles our ON our.role_id = rp.role_id
            INNER JOIN organization_users ou ON ou.id = our.organization_user_id
            WHERE ou.user_id = @UserId
              AND ou.organization_id = @OrganizationId
              AND ou.is_active = TRUE
            """;

        var permissions = await connection.QueryAsync<string>(
            sql,
            new { UserId = userId, OrganizationId = organizationId });

        return permissions.ToHashSet();
    }
}
