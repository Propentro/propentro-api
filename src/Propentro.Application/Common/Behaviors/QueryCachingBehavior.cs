using MediatR;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Application.Common.Behaviors;

public interface ICachedQuery
{
    string CacheKey { get; }
    TimeSpan? CacheDuration => null;
}

public sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : ICachedQuery
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var cached = await cache.GetAsync<TResponse>(request.CacheKey, ct);
        if (cached is not null)
        {
            return cached;
        }

        var response = await next();

        await cache.SetAsync(
            request.CacheKey,
            response,
            request.CacheDuration ?? TimeSpan.FromMinutes(5),
            ct);

        return response;
    }
}
