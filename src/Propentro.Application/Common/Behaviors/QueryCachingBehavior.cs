using MediatR;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches query results for queries implementing ICachedQuery.
/// Automatically prefixes cache keys with "org:{OrganizationId}:" for multi-tenant isolation.
/// </summary>
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cache,
    IUserContext userContext) : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : ICachedQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var cacheKey = GetTenantScopedCacheKey(request.CacheKey);

        var cached = await cache.GetAsync<TResponse>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            Log.Debug("Cache hit: {CacheKey}", cacheKey);
            return cached;
        }

        var result = await next(cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            var expiration = request.Expiration ?? TimeSpan.FromMinutes(5);
            await cache.SetAsync(cacheKey, result.Value, expiration, cancellationToken);
            Log.Debug("Cached: {CacheKey}, Expiration: {Expiration}", cacheKey, expiration);
        }

        return result;
    }

    private string GetTenantScopedCacheKey(string queryCacheKey) =>
        $"org:{userContext.OrganizationId}:{queryCacheKey}";
}
