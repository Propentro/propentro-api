namespace Propentro.Application.Common.Interfaces.Messaging;

/// <summary>
/// Non-generic marker interface for cached queries.
/// Used by the caching behavior to identify cacheable requests.
/// </summary>
public interface ICachedQuery
{
    /// <summary>
    /// The unique cache key for this query.
    /// Should include all query parameters that affect the result.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Optional cache expiration time.
    /// If null, the default expiration from configuration will be used.
    /// </summary>
    TimeSpan? Expiration { get; }
}

/// <summary>
/// Marker interface for queries that support automatic caching.
/// Implement this interface on query records to enable caching via QueryCachingBehavior.
/// </summary>
/// <typeparam name="TResponse">The type of the query response.</typeparam>
/// <example>
/// public record GetUnitsQuery(int Page, int PageSize)
///     : IQuery&lt;PagedResult&lt;UnitDto&gt;&gt;, ICachedQuery&lt;PagedResult&lt;UnitDto&gt;&gt;
/// {
///     public string CacheKey => $"units:list:{Page}:{PageSize}";
///     public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
/// }
/// </example>
public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery;
