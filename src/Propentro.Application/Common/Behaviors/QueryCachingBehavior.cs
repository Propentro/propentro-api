using MediatR;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Behaviors;

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
            return cached;
        }

        var result = await next(cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            await cache.SetAsync(
                request.CacheKey,
                result.Value,
                request.Expiration ?? TimeSpan.FromMinutes(5),
                cancellationToken);
        }

        return result;
    }
}
