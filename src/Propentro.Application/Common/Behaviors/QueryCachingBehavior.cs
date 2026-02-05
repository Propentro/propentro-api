using MediatR;
using Propentro.Application.Common.Interfaces.Caching;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that caches query results for queries implementing ICachedQuery.
/// This behavior uses runtime type checking to conditionally apply caching,
/// avoiding generic constraint issues with MediatR's open behavior registration.
/// </summary>
public sealed class QueryCachingBehavior<TRequest, TResponse>(
    ICacheService cache) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply caching for queries that implement ICachedQuery
        // This runtime check is necessary because MediatR registers open behaviors for all requests
        if (!IsCacheableQuery(request, out var cacheKey, out var expiration))
        {
            return await next(cancellationToken);
        }

        // Try to get from cache first
        var cachedValue = await TryGetFromCacheAsync(cacheKey, cancellationToken);
        if (cachedValue is not null)
        {
            Log.Debug("Cache hit for key: {CacheKey}", cacheKey);
            return cachedValue;
        }

        Log.Debug("Cache miss for key: {CacheKey}", cacheKey);

        // Execute the handler
        var response = await next(cancellationToken);

        // Cache successful results
        await TryCacheResponseAsync(response, cacheKey, expiration, cancellationToken);

        return response;
    }

    private static bool IsCacheableQuery(TRequest request, out string cacheKey, out TimeSpan? expiration)
    {
        cacheKey = string.Empty;
        expiration = null;

        // Check if the request implements ICachedQuery<T> for any T
        var requestType = request.GetType();
        var cachedQueryInterface = requestType
            .GetInterfaces()
            .FirstOrDefault(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(ICachedQuery<>));

        if (cachedQueryInterface is null)
        {
            return false;
        }

        // Get the CacheKey property
        var cacheKeyProperty = cachedQueryInterface.GetProperty(nameof(ICachedQuery<object>.CacheKey));
        var expirationProperty = cachedQueryInterface.GetProperty(nameof(ICachedQuery<object>.Expiration));

        if (cacheKeyProperty is null)
        {
            return false;
        }

        cacheKey = cacheKeyProperty.GetValue(request) as string ?? string.Empty;
        expiration = expirationProperty?.GetValue(request) as TimeSpan?;

        return !string.IsNullOrEmpty(cacheKey);
    }

    private async Task<TResponse?> TryGetFromCacheAsync(string cacheKey, CancellationToken cancellationToken)
    {
        try
        {
            // For Result<T> types, we cache the inner value, not the Result wrapper
            if (IsResultType(out var innerType))
            {
                var cachedInnerValue = await GetCachedValueAsync(innerType, cacheKey, cancellationToken);
                if (cachedInnerValue is not null)
                {
                    return CreateSuccessResult(cachedInnerValue);
                }
            }
            else
            {
                return await cache.GetAsync<TResponse>(cacheKey, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to retrieve value from cache for key: {CacheKey}", cacheKey);
        }

        return default;
    }

    private async Task TryCacheResponseAsync(
        TResponse response,
        string cacheKey,
        TimeSpan? expiration,
        CancellationToken cancellationToken)
    {
        try
        {
            var actualExpiration = expiration ?? TimeSpan.FromMinutes(5);

            // For Result<T> types, only cache successful results
            if (response is Result result)
            {
                if (result.IsFailure)
                {
                    return;
                }

                // Get the inner value from Result<T>
                var valueProperty = response.GetType().GetProperty("Value");
                var value = valueProperty?.GetValue(response);

                if (value is not null)
                {
                    await cache.SetAsync(cacheKey, value, actualExpiration, cancellationToken);
                    Log.Debug("Cached value for key: {CacheKey}, Expiration: {Expiration}", cacheKey, actualExpiration);
                }
            }
            else if (response is not null)
            {
                await cache.SetAsync(cacheKey, response, actualExpiration, cancellationToken);
                Log.Debug("Cached value for key: {CacheKey}, Expiration: {Expiration}", cacheKey, actualExpiration);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to cache value for key: {CacheKey}", cacheKey);
        }
    }

    private static bool IsResultType(out Type? innerType)
    {
        innerType = null;
        var responseType = typeof(TResponse);

        if (!responseType.IsGenericType)
        {
            return false;
        }

        if (responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            return false;
        }

        innerType = responseType.GetGenericArguments()[0];
        return true;
    }

    private async Task<object?> GetCachedValueAsync(Type? innerType, string cacheKey, CancellationToken cancellationToken)
    {
        if (innerType is null)
        {
            return null;
        }

        // Use reflection to call GetAsync<T> with the correct type
        var method = typeof(ICacheService)
            .GetMethod(nameof(ICacheService.GetAsync))!
            .MakeGenericMethod(innerType);

        var task = method.Invoke(cache, [cacheKey, cancellationToken]) as Task;
        if (task is null)
        {
            return null;
        }

        await task.ConfigureAwait(false);

        // Get the result from the completed task
        var resultProperty = task.GetType().GetProperty("Result");
        return resultProperty?.GetValue(task);
    }

    private static TResponse CreateSuccessResult(object innerValue)
    {
        // Create Result<T>.Success(value) using reflection
        var resultType = typeof(TResponse);
        var innerType = resultType.GetGenericArguments()[0];

        // Result<T> has an implicit conversion from T
        // We use the static Create method or constructor
        var implicitOperator = resultType
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == "op_Implicit" &&
                m.ReturnType == resultType &&
                m.GetParameters().Length == 1 &&
                m.GetParameters()[0].ParameterType == innerType);

        if (implicitOperator is not null)
        {
            return (TResponse)implicitOperator.Invoke(null, [innerValue])!;
        }

        // Fallback: Try to find Success method
        var successMethod = typeof(Result)
            .GetMethods()
            .FirstOrDefault(m =>
                m.Name == "Success" &&
                m.IsGenericMethod &&
                m.GetParameters().Length == 1);

        if (successMethod is not null)
        {
            var genericMethod = successMethod.MakeGenericMethod(innerType);
            return (TResponse)genericMethod.Invoke(null, [innerValue])!;
        }

        throw new InvalidOperationException($"Cannot create success Result<{innerType.Name}> from cached value");
    }
}
