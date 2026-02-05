using MediatR;
using Microsoft.Extensions.Logging;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps command execution in a database transaction.
/// Automatically commits on success and rolls back on failure.
/// Only applies to commands (ICommand), not queries.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IAppDbContext dbContext,
    ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Only apply to commands, skip queries
        if (!IsCommand())
        {
            return await next(cancellationToken);
        }

        var requestName = typeof(TRequest).Name;

        await using var transaction = await dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            logger.LogDebug("Beginning transaction for {RequestName}", requestName);

            var response = await next(cancellationToken);

            // Check if the result indicates failure
            if (IsFailureResult(response))
            {
                logger.LogDebug("Rolling back transaction for {RequestName} due to failure result", requestName);
                await transaction.RollbackAsync(cancellationToken);
                return response;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            logger.LogDebug("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Rolling back transaction for {RequestName} due to exception", requestName);
            await transaction.RollbackAsync(cancellationToken);
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
        return response switch
        {
            Result { IsFailure: true } => true,
            _ => false
        };
    }
}
