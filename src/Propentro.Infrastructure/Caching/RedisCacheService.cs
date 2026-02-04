using System.Text.Json;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Propentro.Infrastructure.Caching;

/// <summary>
/// Redis-based distributed cache implementation using StackExchange.Redis.
/// Suitable for multi-instance deployments requiring shared cache state.
/// </summary>
public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<CacheOptions> options) : ICacheService, IAsyncDisposable
{
    private readonly CacheOptions _options = options.Value;
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
    private bool _disposed;

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        var value = await _database.StringGetAsync(prefixedKey);

        return value.HasValue ? Deserialize<T>(value!) : default;
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is null) return;

        var prefixedKey = GetPrefixedKey(key);
        var actualExpiration = expiration ?? _options.DefaultExpiration;
        var serialized = Serialize(value);

        await _database.StringSetAsync(prefixedKey, serialized, actualExpiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        await _database.KeyDeleteAsync(prefixedKey);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var prefixedPattern = GetPrefixedKey(pattern);

        // server.Keys() uses SCAN internally in StackExchange.Redis (lazy enumeration)
        // This is production-safe and efficient for pattern-based key removal
        var endpoints = redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = redis.GetServer(endpoint);
            var keys = server.Keys(pattern: prefixedPattern).ToArray();

            if (keys.Length > 0)
            {
                await _database.KeyDeleteAsync(keys);
            }
        }
    }

    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(factory);

        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null) return existing;

        // Note: For high-concurrency scenarios, consider implementing distributed locking
        // using RedLock (https://redis.io/docs/manual/patterns/distributed-locks/)
        // to prevent cache stampede/thundering herd problem
        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        return await _database.KeyExistsAsync(prefixedKey);
    }

    private string GetPrefixedKey(string key) =>
        string.IsNullOrEmpty(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}{key}";

    private string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, _jsonOptions);

    private T? Deserialize<T>(string value) =>
        string.IsNullOrEmpty(value) ? default : JsonSerializer.Deserialize<T>(value, _jsonOptions);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        await redis.CloseAsync();
        _disposed = true;
    }
}
