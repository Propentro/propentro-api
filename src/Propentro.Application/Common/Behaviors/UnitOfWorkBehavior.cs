using MediatR;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that wraps command execution in a database transaction.
/// Automatically commits on success and rolls back on failure.
/// Only applies to commands (ICommand), not queries.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(
    IAppDbContext dbContext) : IPipelineBehavior<TRequest, TResponse>
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
            Log.Debug("Beginning transaction for {RequestName}", requestName);

            var response = await next(cancellationToken);

            // Check if the result indicates failure
            if (IsFailureResult(response))
            {
                Log.Debug("Rolling back transaction for {RequestName} due to failure result", requestName);
                await transaction.RollbackAsync(cancellationToken);
                return response;
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            Log.Debug("Committed transaction for {RequestName}", requestName);

            return response;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Rolling back transaction for {RequestName} due to exception", requestName);
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
