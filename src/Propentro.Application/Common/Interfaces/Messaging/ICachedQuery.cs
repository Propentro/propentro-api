namespace Propentro.Application.Common.Interfaces.Messaging;

/// <summary>
/// Non-generic marker interface for cached queries.
/// </summary>
public interface ICachedQuery
{
    /// <summary>
    /// The query-specific portion of the cache key (e.g., "units:{Page}:{PageSize}").
    /// The pipeline behavior automatically prefixes this with "org:{OrganizationId}:" for tenant isolation.
    /// Do NOT include OrganizationId in this key — it is added automatically.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Optional cache expiration time. If null, uses default from configuration.
    /// </summary>
    TimeSpan? Expiration { get; }
}

/// <summary>
/// Marker interface for queries that support automatic tenant-scoped caching.
/// The cache key is automatically prefixed with "org:{OrganizationId}:" by QueryCachingBehavior.
/// </summary>
/// <typeparam name="TResponse">The type of the query response.</typeparam>
/// <example>
/// // Organization-scoped cache (most common):
/// public record GetUnitsQuery(int Page, int PageSize)
///     : IQuery&lt;PagedResult&lt;UnitDto&gt;&gt;, ICachedQuery&lt;PagedResult&lt;UnitDto&gt;&gt;
/// {
///     public string CacheKey => $"units:{Page}:{PageSize}";
///     public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
/// }
///
/// // User-scoped cache (include UserId manually):
/// public record GetUserPreferencesQuery(Guid UserId)
///     : IQuery&lt;UserPreferencesDto&gt;, ICachedQuery&lt;UserPreferencesDto&gt;
/// {
///     public string CacheKey => $"user:{UserId}:preferences";
///     public TimeSpan? Expiration => TimeSpan.FromMinutes(15);
/// }
/// </example>
public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery;
