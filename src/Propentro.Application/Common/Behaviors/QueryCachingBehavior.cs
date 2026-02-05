using MediatR;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches query results for queries implementing ICachedQuery.
/// </summary>
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cache) : IPipelineBehavior<TRequest, Result<TResponse>>
    where TRequest : ICachedQuery<TResponse>
{
    public async Task<Result<TResponse>> Handle(
        TRequest request,
        RequestHandlerDelegate<Result<TResponse>> next,
        CancellationToken cancellationToken)
    {
        var cached = await cache.GetAsync<TResponse>(request.CacheKey, cancellationToken);
        if (cached is not null)
        {
            Log.Debug("Cache hit: {CacheKey}", request.CacheKey);
            return cached;
        }

        var result = await next(cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            var expiration = request.Expiration ?? TimeSpan.FromMinutes(5);
            await cache.SetAsync(request.CacheKey, result.Value, expiration, cancellationToken);
            Log.Debug("Cached: {CacheKey}, Expiration: {Expiration}", request.CacheKey, expiration);
        }

        return result;
    }
}
