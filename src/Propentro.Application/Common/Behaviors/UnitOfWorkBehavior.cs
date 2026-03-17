using MediatR;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps command execution in a database transaction.
/// Automatically calls SaveChangesAsync and commits on success, rolls back on failure.
/// Handlers should NOT call SaveChangesAsync — this behavior handles persistence.
/// After successful commit, invalidates cache keys if the request implements <see cref="ICacheInvalidating"/>.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IAppDbContext dbContext,
    ICacheService cacheService,
    IUserContext userContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand())
        {
            return await next(cancellationToken);
        }

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            Log.Debug("Transaction started for {RequestName}", typeof(TRequest).Name);

            var response = await next(cancellationToken);

            if (IsFailureResult(response))
            {
                await transaction.RollbackAsync(cancellationToken);
                Log.Debug("Transaction rolled back for {RequestName} - failure result", typeof(TRequest).Name);
                return response;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            Log.Debug("Transaction committed for {RequestName}", typeof(TRequest).Name);

            await InvalidateCacheAsync(request, cancellationToken);

            return response;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            Log.Error(ex, "Transaction rolled back for {RequestName} - exception", typeof(TRequest).Name);
            throw;
        }
    }

    private static bool IsCommand()
    {
        return typeof(TRequest).GetInterfaces().Any(i =>
            i == typeof(ICommand) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));
    }

    private static bool IsFailureResult(TResponse response)
    {
        return response is Result { IsFailure: true };
    }

    private async Task InvalidateCacheAsync(TRequest request, CancellationToken cancellationToken)
    {
        if (request is not ICacheInvalidating cacheInvalidating)
        {
            return;
        }

        var cacheKeys = cacheInvalidating.CacheKeysToInvalidate.Select(key =>
            cacheInvalidating.KeysAreFullyQualified
                ? key
                : $"org:{userContext.OrganizationId}:{key}");

        foreach (var cacheKey in cacheKeys)
        {
            if (cacheKey.Contains('*'))
                await cacheService.RemoveByPatternAsync(cacheKey, cancellationToken);
            else
                await cacheService.RemoveAsync(cacheKey, cancellationToken);

            Log.Debug("Cache invalidated for key {CacheKey} after {RequestName}", cacheKey, typeof(TRequest).Name);
        }
    }
}
