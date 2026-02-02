using Dapper;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Services;

public sealed class PermissionService(
    IDbConnectionFactory db,
    ICacheService cache) : IPermissionService
{
    public async Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken ct = default)
    {
        var cacheKey = $"permissions:{organizationId}:{userId}";

        var cached = await cache.GetAsync<HashSet<string>>(cacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        using var connection = db.CreateConnection();

        const string sql = """
            SELECT DISTINCT p.code
            FROM organization_user_roles our
            JOIN role_permissions rp ON rp.role_id = our.role_id
            JOIN permissions p ON p.id = rp.permission_id
            JOIN roles r ON r.id = our.role_id
            WHERE our.user_id = @UserId
              AND our.organization_id = @OrganizationId
              AND r.is_active = TRUE
            """;

        var permissions = await connection.QueryAsync<string>(
            sql,
            new { UserId = userId, OrganizationId = organizationId });

        var result = permissions.ToHashSet();

        await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);

        return result;
    }
}
