using Dapper;
using Microsoft.Extensions.Caching.Memory;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Resolves Clerk authentication provider IDs to internal database IDs.
/// Uses IMemoryCache for caching to avoid repeated database lookups.
/// </summary>
public sealed class ClerkIdResolver : IExternalIdResolver
{
    private readonly IDbConnectionFactory _db;
    private readonly IMemoryCache _cache;

    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    // Cache key prefixes for different ID types
    private const string OrgIdCachePrefix = "identity:org:id:";
    private const string UserIdCachePrefix = "identity:user:id:";
    private const string UserPublicIdCachePrefix = "identity:user:public:";

    public ClerkIdResolver(IDbConnectionFactory db, IMemoryCache cache)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<Guid> ResolveOrganizationIdAsync(string externalOrgId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalOrgId))
        {
            return Guid.Empty;
        }

        var cacheKey = $"{OrgIdCachePrefix}{externalOrgId}";

        if (_cache.TryGetValue<Guid>(cacheKey, out var cached))
        {
            return cached;
        }

        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT id
            FROM organizations
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var id = await connection.QueryFirstOrDefaultAsync<Guid?>(
            sql,
            new { ExternalId = externalOrgId, AuthProvider = "clerk" });

        if (!id.HasValue)
        {
            return Guid.Empty;
        }

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1);

        _cache.Set(cacheKey, id.Value, cacheOptions);
        return id.Value;
    }

    public async Task<Guid> ResolveUserIdAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
        {
            return Guid.Empty;
        }

        var cacheKey = $"{UserIdCachePrefix}{externalUserId}";

        if (_cache.TryGetValue<Guid>(cacheKey, out var cached))
        {
            return cached;
        }

        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT id
            FROM users
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var id = await connection.QueryFirstOrDefaultAsync<Guid?>(
            sql,
            new { ExternalId = externalUserId, AuthProvider = "clerk" });

        if (!id.HasValue)
        {
            return Guid.Empty;
        }

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1);

        _cache.Set(cacheKey, id.Value, cacheOptions);
        return id.Value;
    }

    public async Task<string> ResolvePublicUserIdAsync(string externalUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(externalUserId))
        {
            return string.Empty;
        }

        var cacheKey = $"{UserPublicIdCachePrefix}{externalUserId}";

        if (_cache.TryGetValue<string>(cacheKey, out var cached) && !string.IsNullOrEmpty(cached))
        {
            return cached;
        }

        using var connection = _db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT public_id
            FROM users
            WHERE external_id = @ExternalId
              AND auth_provider = @AuthProvider
              AND is_active = true
            """;

        var publicId = await connection.QueryFirstOrDefaultAsync<string?>(
            sql,
            new { ExternalId = externalUserId, AuthProvider = "clerk" });

        if (string.IsNullOrEmpty(publicId))
        {
            return string.Empty;
        }

        var cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(CacheDuration)
            .SetSize(1);

        _cache.Set(cacheKey, publicId, cacheOptions);
        return publicId;
    }
}
