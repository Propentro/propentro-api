using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Serilog;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Resolves Clerk authentication provider IDs to internal database IDs.
/// Uses IMemoryCache intentionally for identity resolution caching - this data
/// should remain local to each instance and not be shared across distributed cache.
/// </summary>
public sealed class ClerkIdResolver(IDbConnectionFactory db, IMemoryCache cache) : IExternalIdResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    private const string OrgCachePrefix = "identity:org:";
    private const string UserCachePrefix = "identity:user:";
    private const string MembershipCachePrefix = "identity:membership:";

    public async Task<ResolvedIdentity> ResolveIdentityAsync(
        string? externalUserId,
        string? externalOrgId,
        CancellationToken cancellationToken = default)
    {
        // Phase 1: Check caches
        var (cachedUserId, cachedPublicUserId, userCacheHit) = TryGetUserFromCache(externalUserId);
        var (cachedOrgId, cachedPublicOrgId, orgCacheHit) = TryGetOrgFromCache(externalOrgId);

        // Fast path: user and org both cached
        if (userCacheHit && orgCacheHit)
        {
            // If either is empty, no membership check needed
            if (cachedUserId == Guid.Empty || cachedOrgId == Guid.Empty)
                return new ResolvedIdentity(cachedUserId, cachedPublicUserId, cachedOrgId, cachedPublicOrgId);

            // Check membership cache — avoid DB connection if possible
            if (TryGetMembershipFromCache(cachedUserId, cachedOrgId, out var isMember))
            {
                return isMember
                    ? new ResolvedIdentity(cachedUserId, cachedPublicUserId, cachedOrgId, cachedPublicOrgId)
                    : new ResolvedIdentity(cachedUserId, cachedPublicUserId, Guid.Empty, string.Empty);
            }
        }

        // Phase 2: Open single connection for all uncached queries
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        // Run queries sequentially — Npgsql does not support concurrent commands on a single connection
        var (userId, publicUserId) = userCacheHit
            ? (cachedUserId, cachedPublicUserId)
            : await ResolveUserAsync(connection, externalUserId);

        var (orgId, publicOrgId) = orgCacheHit
            ? (cachedOrgId, cachedPublicOrgId)
            : await ResolveOrgAsync(connection, externalOrgId);

        // Phase 3: Verify user-org membership
        if (userId != Guid.Empty && orgId != Guid.Empty)
        {
            var memberActive = await VerifyMembershipAsync(connection, userId, orgId);
            if (!memberActive)
            {
                Log.Warning(
                    "User {UserId} is not an active member of organization {OrganizationId}",
                    userId, orgId);
                return new ResolvedIdentity(userId, publicUserId, Guid.Empty, string.Empty);
            }
        }

        return new ResolvedIdentity(userId, publicUserId, orgId, publicOrgId);
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

    private (Guid OrgId, string PublicOrgId, bool CacheHit) TryGetOrgFromCache(string? externalOrgId)
    {
        if (string.IsNullOrWhiteSpace(externalOrgId))
            return (Guid.Empty, string.Empty, true);

        var cacheKey = $"{OrgCachePrefix}{externalOrgId}";

        if (cache.TryGetValue<(Guid, string)>(cacheKey, out var cached))
            return (cached.Item1, cached.Item2, true);

        return (Guid.Empty, string.Empty, false);
    }

    private bool TryGetMembershipFromCache(Guid userId, Guid orgId, out bool isMember)
    {
        var cacheKey = $"{MembershipCachePrefix}{userId}:{orgId}";
        return cache.TryGetValue(cacheKey, out isMember);
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

        var result = await connection.QueryFirstOrDefaultAsync<IdResult>(
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

    private async Task<(Guid OrgId, string PublicOrgId)> ResolveOrgAsync(
        System.Data.Common.DbConnection connection,
        string? externalOrgId)
    {
        if (string.IsNullOrWhiteSpace(externalOrgId))
            return (Guid.Empty, string.Empty);

        const string sql = """
            SELECT id, public_id
            FROM organizations
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var result = await connection.QueryFirstOrDefaultAsync<IdResult>(
            sql, new { ExternalId = externalOrgId, AuthProvider = "clerk" });

        if (result is null)
            return (Guid.Empty, string.Empty);

        // Cache the combined result
        var cacheKey = $"{OrgCachePrefix}{externalOrgId}";
        cache.Set(cacheKey, (result.Id, result.PublicId), new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));

        return (result.Id, result.PublicId);
    }

    private async Task<bool> VerifyMembershipAsync(
        System.Data.Common.DbConnection connection,
        Guid userId,
        Guid orgId)
    {
        var cacheKey = $"{MembershipCachePrefix}{userId}:{orgId}";
        if (cache.TryGetValue<bool>(cacheKey, out var cached))
            return cached;

        const string sql = """
            SELECT 1
            FROM organization_users
            WHERE user_id = @UserId
              AND organization_id = @OrgId
              AND status = 'active'
            LIMIT 1
            """;

        var result = await connection.QueryFirstOrDefaultAsync<int?>(
            sql, new { UserId = userId, OrgId = orgId });

        var isMember = result.HasValue;

        cache.Set(cacheKey, isMember, new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1));

        return isMember;
    }

    private sealed record IdResult(Guid Id, string PublicId);
}
