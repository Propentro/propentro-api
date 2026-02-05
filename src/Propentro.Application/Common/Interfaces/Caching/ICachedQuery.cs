namespace Propentro.Application.Common.Interfaces.Caching;

/// <summary>
/// Marker interface for queries that should have their results cached.
/// Implement this interface on query records to enable automatic caching via QueryCachingBehavior.
/// </summary>
/// <typeparam name="TResponse">The type of the query response to be cached.</typeparam>
public interface ICachedQuery<TResponse>
{
    /// <summary>
    /// Gets the cache key for this query.
    /// IMPORTANT: For multi-tenant applications, include the organization context in the key.
    /// Example: $"properties:{organizationId}:{page}:{pageSize}"
    /// </summary>
    string CacheKey { get; }

    /// <summary>
    /// Gets the cache expiration time.
    /// Return null to use the default expiration from CacheOptions.
    /// </summary>
    TimeSpan? Expiration { get; }
}

/// <summary>
/// Extended interface for cached queries that need organization-scoped caching.
/// Provides a standardized way to build cache keys that include organization context.
/// </summary>
/// <typeparam name="TResponse">The type of the query response to be cached.</typeparam>
public interface IOrganizationCachedQuery<TResponse> : ICachedQuery<TResponse>
{
    /// <summary>
    /// Gets the base cache key without organization prefix.
    /// The full CacheKey will be automatically prefixed with organization ID.
    /// </summary>
    string BaseCacheKey { get; }

    /// <summary>
    /// Gets the organization ID for cache key scoping.
    /// This should match the current user's organization context.
    /// </summary>
    Guid OrganizationId { get; }

    /// <summary>
    /// Gets the fully qualified cache key including organization scope.
    /// Default implementation: "org:{OrganizationId}:{BaseCacheKey}"
    /// </summary>
    string ICachedQuery<TResponse>.CacheKey => $"org:{OrganizationId}:{BaseCacheKey}";
}
