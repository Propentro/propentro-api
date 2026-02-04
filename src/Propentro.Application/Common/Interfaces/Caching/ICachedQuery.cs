namespace Propentro.Application.Common.Interfaces.Caching;

/// <summary>
/// Marker interface for queries that support caching.
/// Implement this interface on query records to enable automatic caching via QueryCachingBehavior.
/// </summary>
/// <typeparam name="TResponse">The type of the query response.</typeparam>
public interface ICachedQuery<TResponse>
{
    /// <summary>
    /// The unique cache key for this query.
    /// Should include all query parameters that affect the result.
    /// </summary>
    /// <example>
    /// public string CacheKey => $"units:list:{OrganizationId}:{Page}:{PageSize}";
    /// </example>
    string CacheKey { get; }

    /// <summary>
    /// Optional cache expiration time.
    /// If null, the default expiration from configuration will be used.
    /// </summary>
    TimeSpan? Expiration => null;
}
