using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Propentro.Infrastructure.Caching;

/// <summary>
/// In-memory cache implementation using Microsoft.Extensions.Caching.Memory.
/// Thread-safe and suitable for single-instance deployments.
/// </summary>
public sealed class MemoryCacheService(
    IMemoryCache cache,
    IOptions<CachingOptions> cachingOptions) : ICacheService, IDisposable
{
    private readonly string _keyPrefix = cachingOptions.Value.KeyPrefix;
    private readonly TimeSpan _defaultExpiration = cachingOptions.Value.DefaultExpiration;
    private readonly ConcurrentDictionary<string, byte> _keys = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private bool _disposed;

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        var value = cache.TryGetValue(prefixedKey, out T? result) ? result : default;

        return Task.FromResult(value);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (value is null) return Task.CompletedTask;

        var prefixedKey = GetPrefixedKey(key);
        var actualExpiration = expiration ?? _defaultExpiration;

        var cacheEntryOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(actualExpiration)
            .SetSize(1)
            .RegisterPostEvictionCallback(OnCacheEntryEvicted);

        cache.Set(prefixedKey, value, cacheEntryOptions);
        _keys.TryAdd(prefixedKey, 0);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        cache.Remove(prefixedKey);
        _keys.TryRemove(prefixedKey, out _);

        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pattern);

        var prefixedPattern = GetPrefixedKey(pattern);
        var regex = WildcardToRegex(prefixedPattern);

        var keysToRemove = _keys.Keys.Where(k => regex.IsMatch(k)).ToList();

        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
            _keys.TryRemove(key, out _);
        }

        return Task.CompletedTask;
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

        // Slow path: acquire per-key lock to prevent cache stampede
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

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        return Task.FromResult(cache.TryGetValue(prefixedKey, out _));
    }

    private string GetPrefixedKey(string key) => $"{_keyPrefix}{key}";

    private void OnCacheEntryEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (key is string keyString)
            _keys.TryRemove(keyString, out _);
    }

    private static Regex WildcardToRegex(string pattern)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
    }

    public void Dispose()
    {
        if (_disposed) return;

        foreach (var semaphore in _locks.Values)
        {
            semaphore.Dispose();
        }

        _locks.Clear();
        _keys.Clear();
        _disposed = true;
    }
}
