namespace Propentro.Application.Common.Interfaces.Messaging;

/// <summary>
/// Non-generic marker interface for cached queries.
/// </summary>
public interface ICachedQuery
{
    /// <summary>
    /// The unique cache key for this query.
    /// IMPORTANT: For multi-tenant apps, include OrganizationId in the key.
    /// For user-specific data, include UserId in the key.
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Optional cache expiration time. If null, uses default from configuration.
    /// </summary>
    TimeSpan? Expiration { get; }
}

/// <summary>
/// Marker interface for queries that support automatic caching.
/// </summary>
/// <typeparam name="TResponse">The type of the query response.</typeparam>
/// <example>
/// // Organization-scoped cache (most common):
/// public record GetUnitsQuery(Guid OrganizationId, int Page, int PageSize)
///     : IQuery&lt;PagedResult&lt;UnitDto&gt;&gt;, ICachedQuery&lt;PagedResult&lt;UnitDto&gt;&gt;
/// {
///     public string CacheKey => $"org:{OrganizationId}:units:{Page}:{PageSize}";
///     public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
/// }
///
/// // User-scoped cache:
/// public record GetUserPreferencesQuery(Guid UserId)
///     : IQuery&lt;UserPreferencesDto&gt;, ICachedQuery&lt;UserPreferencesDto&gt;
/// {
///     public string CacheKey => $"user:{UserId}:preferences";
///     public TimeSpan? Expiration => TimeSpan.FromMinutes(15);
/// }
/// </example>
public interface ICachedQuery<TResponse> : IQuery<TResponse>, ICachedQuery;
