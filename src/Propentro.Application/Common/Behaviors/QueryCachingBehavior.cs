using MediatR;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Interfaces.Caching;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches query results for queries implementing ICachedQuery.
/// </summary>
/// <typeparam name="TRequest">The query type.</typeparam>
/// <typeparam name="TResponse">The response type.</typeparam>
public sealed class QueryCachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : ICachedQuery<TResponse>
{
    private readonly Caching.ICacheService _cache;

    public QueryCachingBehavior(Caching.ICacheService cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        // Try to get from cache
        var cached = await _cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        // Execute the query
        var result = await next(cancellationToken);

        // Cache successful results
        if (result.IsSuccess && result.Value is not null)
        {
            var expiration = request.Expiration ?? TimeSpan.FromMinutes(5);
            await _cache.SetAsync(request.CacheKey, result.Value, expiration, cancellationToken);
        }

        return result;
    }
}
