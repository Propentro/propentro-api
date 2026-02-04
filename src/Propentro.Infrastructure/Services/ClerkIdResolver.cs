using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Services;

public sealed class ClerkIdResolver(IDbConnectionFactory db, IMemoryCache cache) : IClerkIdResolver
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    public async Task<Guid> ResolveOrganizationIdAsync(string clerkOrgId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(clerkOrgId))
        {
            return Guid.Empty;
        }

        string cacheKey = $"clerk:org:{clerkOrgId}";

        if (cache.TryGetValue<Guid>(cacheKey, out var cached))
        {
            return cached;
        }

        await using var connection = db.CreateConnection();

        var id = await connection.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT id FROM organizations WHERE external_id = @ClerkId AND auth_provider = @AuthProvider AND is_active = true",
            new { ClerkId = clerkOrgId, AuthProvider = "clerk" });

        if (!id.HasValue)
        {
            return Guid.Empty;
        }

        cache.Set(cacheKey, id.Value, CacheDuration);
        return id.Value;

    }

    public async Task<Guid> ResolveUserIdAsync(string clerkUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(clerkUserId))
        {
            return Guid.Empty;
        }

        string cacheKey = $"clerk:user:{clerkUserId}";

        if (cache.TryGetValue<Guid>(cacheKey, out var cached))
        {
            return cached;
        }

        using var connection = db.CreateConnection();

        var id = await connection.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT id FROM users WHERE external_id = @ClerkId AND auth_provider = @AuthProvider AND is_active = true",
            new { ClerkId = clerkUserId, AuthProvider = "clerk" });

        if (!id.HasValue)
        {
            return Guid.Empty;
        }

        cache.Set(cacheKey, id.Value, CacheDuration);
        return id.Value;

    }

    public Task<string> ResolvePublicUserIdAsync(string clerkUserId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(clerkUserId))
        {
            return string.Empty;
        }

        string cacheKey = $"clerk:user:{clerkUserId}";

        if (cache.TryGetValue<string>(cacheKey, out var cached))
        {
            return cached;
        }

        using var connection = db.CreateConnection();

        var id = await connection.QueryFirstOrDefaultAsync<Guid?>(
            "SELECT public_id FROM users WHERE external_id = @ClerkId AND auth_provider = @AuthProvider AND is_active = true",
            new { ClerkId = clerkUserId, AuthProvider = "clerk" });

        if (!id.HasValue)
        {
            return string.Empty;
        }

        cache.Set(cacheKey, id.Value, CacheDuration);
        return id;

    }

}
