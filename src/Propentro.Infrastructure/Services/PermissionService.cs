using Dapper;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Provides permission lookup for authorization purposes.
/// Permissions are cached for performance and can be invalidated when role assignments change.
/// </summary>
public sealed class PermissionService : IPermissionService
{
    private readonly IDbConnectionFactory _db;
    private readonly ICacheService _cache;

    private const string CacheKeyPrefix = "permissions:";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public PermissionService(IDbConnectionFactory db, ICacheService cache)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    /// <inheritdoc />
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

        var cached = await _cache.GetAsync<HashSet<string>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT DISTINCT p.code
            FROM organization_user_roles our
            JOIN role_permissions rp ON rp.role_id = our.role_id
            JOIN permissions p ON p.id = rp.permission_id
            JOIN roles r ON r.id = our.role_id
            WHERE our.user_id = @UserId
              AND our.organization_id = @OrganizationId
              AND r.is_active = TRUE
              AND p.is_active = TRUE
            """;

        var permissions = await connection.QueryAsync<string>(
            sql,
            new { UserId = userId, OrganizationId = organizationId });

        var result = permissions.ToHashSet();

        await _cache.SetAsync(cacheKey, result, CacheExpiration, cancellationToken);

        return result;
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task InvalidatePermissionsCacheAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = GetCacheKey(userId, organizationId);
        await _cache.RemoveAsync(cacheKey, cancellationToken);
    }

    private static string GetCacheKey(Guid userId, Guid organizationId)
    {
        return $"{CacheKeyPrefix}{organizationId}:{userId}";
    }
}
