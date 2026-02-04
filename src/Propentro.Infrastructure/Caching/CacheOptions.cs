namespace Propentro.Infrastructure.Caching;

/// <summary>
/// Cache provider types.
/// </summary>
public enum CacheProvider
{
    /// <summary>
    /// In-memory cache. Suitable for single-instance deployments.
    /// </summary>
    Memory,

    /// <summary>
    /// Distributed Redis cache. Suitable for multi-instance deployments.
    /// </summary>
    Redis
}

/// <summary>
/// Configuration options for the caching layer.
/// Bind from appsettings.json under the "Caching" section.
/// </summary>
public sealed class CacheOptions
{
    public const string SectionName = "Caching";

    /// <summary>
    /// The cache provider to use. Default: Memory
    /// </summary>
    public CacheProvider Provider { get; set; } = CacheProvider.Memory;

    /// <summary>
    /// Default expiration for cached items. Default: 5 minutes
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Memory cache size limit. Default: 1024
    /// </summary>
    public int MemoryCacheSizeLimit { get; set; } = 1024;

    /// <summary>
    /// Memory cache expiration scan frequency. Default: 1 minute
    /// </summary>
    public TimeSpan MemoryCacheExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Redis connection string. Required when Provider is Redis.
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Prefix for all cache keys. Default: "propentro:"
    /// </summary>
    public string KeyPrefix { get; set; } = "propentro:";
}
