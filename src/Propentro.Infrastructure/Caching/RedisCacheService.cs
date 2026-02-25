using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Serilog;
using StackExchange.Redis;

namespace Propentro.Infrastructure.Caching;

/// <summary>
/// Redis-based distributed cache implementation using StackExchange.Redis.
/// Suitable for multi-instance deployments requiring shared cache state.
/// Uses per-key SemaphoreSlim for local stampede protection in GetOrCreateAsync.
/// For cross-instance protection, consider RedLock distributed locking.
/// </summary>
public sealed class RedisCacheService(
    IConnectionMultiplexer redis,
    IOptions<CachingOptions> cachingOptions) : ICacheService, IAsyncDisposable
{
    private readonly string _keyPrefix = cachingOptions.Value.KeyPrefix;
    private readonly TimeSpan _defaultExpiration = cachingOptions.Value.DefaultExpiration;
    private readonly IDatabase _database = redis.GetDatabase();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
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
        var actualExpiration = expiration ?? _defaultExpiration;
        var serialized = Serialize(value);

        await _database.StringSetAsync(prefixedKey, serialized, actualExpiration);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        try
        {
            var prefixedKey = GetPrefixedKey(key);
            await _database.KeyDeleteAsync(prefixedKey);
        }
        catch (RedisConnectionException ex)
        {
            Log.Warning(ex, "Redis unavailable during cache removal for key {CacheKey} — skipping (best-effort)", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        try
        {
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
        catch (RedisConnectionException ex)
        {
            Log.Warning(ex, "Redis unavailable during cache removal for pattern {CachePattern} — skipping (best-effort)", pattern);
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

        // Fast path: check cache without acquiring lock
        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null) return existing;

        // Slow path: acquire per-key lock for local stampede protection.
        // For cross-instance protection, consider RedLock distributed locking.
        var keyLock = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));

        await keyLock.WaitAsync(cancellationToken);
        try
        {
            // Double-check after acquiring lock
            existing = await GetAsync<T>(key, cancellationToken);
            if (existing is not null) return existing;

            var value = await factory(cancellationToken);
            if (value is not null)
            {
                await SetAsync(key, value, expiration, cancellationToken);
            }

            return value;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        return await _database.KeyExistsAsync(prefixedKey);
    }

    private string GetPrefixedKey(string key) => $"{_keyPrefix}{key}";

    private string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, _jsonOptions);

    private T? Deserialize<T>(string value) =>
        string.IsNullOrEmpty(value) ? default : JsonSerializer.Deserialize<T>(value, _jsonOptions);

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }

        _locks.Clear();

        await redis.CloseAsync();
        _disposed = true;
    }
}
