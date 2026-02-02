using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Caching;

public sealed class CacheService(IDistributedCache cache) : ICacheService
{
    private readonly IDistributedCache _cache = cache;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var cached = await _cache.GetStringAsync(key, ct);

        if (string.IsNullOrEmpty(cached))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(cached, JsonOptions);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken ct = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5)
        };

        var json = JsonSerializer.Serialize(value, JsonOptions);
        await _cache.SetStringAsync(key, json, options, ct);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await _cache.RemoveAsync(key, ct);
    }
}
