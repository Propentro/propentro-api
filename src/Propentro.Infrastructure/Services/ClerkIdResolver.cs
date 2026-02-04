using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Propentro.Application.Common.Interfaces.Identity;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Resolves Clerk authentication provider IDs to internal database IDs.
/// Uses IMemoryCache intentionally for identity resolution caching - this data
/// should remain local to each instance and not be shared across distributed cache.
/// </summary>
public sealed class ClerkIdResolver(IDbConnectionFactory db, IMemoryCache cache) : IExternalIdResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private const string OrgIdCachePrefix = "identity:org:id:";
    private const string UserCachePrefix = "identity:user:";

    public async Task<ResolvedIdentity> ResolveIdentityAsync(
        string? externalUserId,
        string? externalOrgId,
        CancellationToken cancellationToken = default)
    {
        // Check cache first for all values
        var (cachedUserId, cachedPublicUserId, userCacheHit) = TryGetUserFromCache(externalUserId);
        var (cachedOrgId, orgCacheHit) = TryGetOrgFromCache(externalOrgId);

        // If all values are cached, return immediately
        if (userCacheHit && orgCacheHit)
        {
            return new ResolvedIdentity(cachedUserId, cachedPublicUserId, cachedOrgId);
        }

        // Open single connection for all uncached queries
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Run queries in parallel for uncached values
        var userTask = userCacheHit
            ? Task.FromResult((cachedUserId, cachedPublicUserId))
            : ResolveUserAsync(connection, externalUserId);

        var orgTask = orgCacheHit
            ? Task.FromResult(cachedOrgId)
            : ResolveOrgAsync(connection, externalOrgId);

        await Task.WhenAll(userTask, orgTask);

        var (userId, publicUserId) = await userTask;
        var orgId = await orgTask;

        return new ResolvedIdentity(userId, publicUserId, orgId);
    }

    private (Guid UserId, string PublicUserId, bool CacheHit) TryGetUserFromCache(string? externalUserId)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            return (Guid.Empty, string.Empty, true);

        var cacheKey = $"{UserCachePrefix}{externalUserId}";

        if (cache.TryGetValue<(Guid, string)>(cacheKey, out var cached))
            return (cached.Item1, cached.Item2, true);

        return (Guid.Empty, string.Empty, false);
    }

    private (Guid OrgId, bool CacheHit) TryGetOrgFromCache(string? externalOrgId)
    {
        if (string.IsNullOrWhiteSpace(externalOrgId))
            return (Guid.Empty, true);

        var cacheKey = $"{OrgIdCachePrefix}{externalOrgId}";

        if (cache.TryGetValue<Guid>(cacheKey, out var cached))
            return (cached, true);

        return (Guid.Empty, false);
    }

    private async Task<(Guid UserId, string PublicUserId)> ResolveUserAsync(
        System.Data.Common.DbConnection connection,
        string? externalUserId)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
            return (Guid.Empty, string.Empty);

        const string sql = """
            SELECT id, public_id
            FROM users
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<UserIdResult>(
            sql, new { ExternalId = externalUserId, AuthProvider = "clerk" });

        if (result is null)
            return (Guid.Empty, string.Empty);

        // Cache the combined result
        var cacheKey = $"{UserCachePrefix}{externalUserId}";
        cache.Set(cacheKey, (result.Id, result.PublicId), new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));

        return (result.Id, result.PublicId);
    }

    private async Task<Guid> ResolveOrgAsync(
        System.Data.Common.DbConnection connection,
        string? externalOrgId)
    {
        if (string.IsNullOrWhiteSpace(externalOrgId))
            return Guid.Empty;

        const string sql = """
            SELECT id FROM organizations
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var id = await connection.QueryFirstOrDefaultAsync<Guid?>(
            sql, new { ExternalId = externalOrgId, AuthProvider = "clerk" });

        if (!id.HasValue)
            return Guid.Empty;

        // Cache the result
        var cacheKey = $"{OrgIdCachePrefix}{externalOrgId}";
        cache.Set(cacheKey, id.Value, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));

        return id.Value;
    }

    private sealed record UserIdResult(Guid Id, string PublicId);
}
