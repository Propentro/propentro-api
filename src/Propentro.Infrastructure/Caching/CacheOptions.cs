namespace Propentro.Infrastructure.Caching;

/// <summary>
/// Configuration options for the caching layer.
/// Can be bound from appsettings.json under the "Caching" section.
/// </summary>
public sealed class CacheOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json.
    /// </summary>
    public const string SectionName = "Caching";

    /// <summary>
    /// The cache provider to use. Options: "Memory", "Redis".
    /// Default: "Memory"
    /// </summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>
    /// Default expiration time for cached items when not explicitly specified.
    /// Default: 5 minutes
    /// </summary>
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum number of items to keep in memory cache.
    /// Only applies to Memory provider.
    /// Default: 1024
    /// </summary>
    public int MemoryCacheSizeLimit { get; set; } = 1024;

    /// <summary>
    /// How often to scan for expired items.
    /// Only applies to Memory provider.
    /// Default: 1 minute
    /// </summary>
    public TimeSpan MemoryCacheExpirationScanFrequency { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Redis connection string.
    /// Only applies when Provider is "Redis".
    /// </summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Prefix for all cache keys to avoid collisions with other applications.
    /// Default: "propentro:"
    /// </summary>
    public string KeyPrefix { get; set; } = "propentro:";
}
