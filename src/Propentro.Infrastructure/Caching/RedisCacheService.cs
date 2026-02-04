using System.Text.Json;
using Microsoft.Extensions.Options;
using Propentro.Application.Common.Interfaces.Caching;
using StackExchange.Redis;

namespace Propentro.Infrastructure.Caching;

/// <summary>
/// Redis-based distributed cache implementation using StackExchange.Redis.
/// Suitable for multi-instance deployments requiring shared cache state.
/// </summary>
/// <remarks>
/// To use this implementation:
/// 1. Install StackExchange.Redis NuGet package
/// 2. Configure Redis connection string in appsettings.json
/// 3. Set Caching:Provider to "Redis" in configuration
/// </remarks>
public sealed class RedisCacheService : ICacheService, IAsyncDisposable
{
    private readonly CacheOptions _options;
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _database;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    public RedisCacheService(IConnectionMultiplexer redis, IOptions<CacheOptions> options)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options?.Value ?? new CacheOptions();
        _database = _redis.GetDatabase();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        var value = await _database.StringGetAsync(prefixedKey);

        if (!value.HasValue)
        {
            return default;
        }

        return Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is null)
        {
            return;
        }

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

        // Use SCAN to find matching keys (more efficient than KEYS in production)
        var endpoints = _redis.GetEndPoints();
        foreach (var endpoint in endpoints)
        {
            var server = _redis.GetServer(endpoint);
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
        if (existing is not null)
        {
            return existing;
        }

        // Note: For production, consider using RedLock for distributed locking
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

    private string GetPrefixedKey(string key)
    {
        return string.IsNullOrEmpty(_options.KeyPrefix)
            ? key
            : $"{_options.KeyPrefix}{key}";
    }

    private string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, _jsonOptions);
    }

    private T? Deserialize<T>(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value, _jsonOptions);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await _redis.CloseAsync();
        _disposed = true;
    }
}
