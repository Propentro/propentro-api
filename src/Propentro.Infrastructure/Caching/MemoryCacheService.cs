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
    IOptions<CacheOptions> options) : ICacheService, IDisposable
{
    private readonly CacheOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, byte> _keys = new();
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
        var actualExpiration = expiration ?? _options.DefaultExpiration;

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

        var existing = await GetAsync<T>(key, cancellationToken);
        if (existing is not null) return existing;

        var value = await factory(cancellationToken);
        if (value is not null)
        {
            await SetAsync(key, value, expiration, cancellationToken);
        }

        return value;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var prefixedKey = GetPrefixedKey(key);
        return Task.FromResult(cache.TryGetValue(prefixedKey, out _));
    }

    private string GetPrefixedKey(string key) =>
        string.IsNullOrEmpty(_options.KeyPrefix) ? key : $"{_options.KeyPrefix}{key}";

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
        _keys.Clear();
        _disposed = true;
    }
}
