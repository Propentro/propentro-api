using Dapper;
using Propentro.Application.Common.Interfaces.Authorization;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Provides permission lookup for authorization purposes.
/// Uses GetOrCreateAsync for stampede-protected caching.
/// </summary>
public sealed class PermissionService(IDbConnectionFactory db, ICacheService cache) : IPermissionService
{
    private static readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5);

    public async Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty || organizationId == Guid.Empty)
        {
            return [];
        }

        var cacheKey = GetCacheKey(userId, organizationId);

        var result = await cache.GetOrCreateAsync(
            cacheKey,
            async ct =>
            {
                await using var connection = db.CreateConnection();
                await connection.OpenAsync(ct);

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

                return permissions.ToHashSet();
            },
            _cacheExpiration,
            cancellationToken);

        return result ?? [];
    }

    public async Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return false;
        }

        var permissions = await GetUserPermissionsAsync(userId, organizationId, cancellationToken);
        return permissions.Contains(permissionCode);
    }

    private static string GetCacheKey(Guid userId, Guid organizationId) =>
        $"org:{organizationId}:user-permissions:{userId}";
}
