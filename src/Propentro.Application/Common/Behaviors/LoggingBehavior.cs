using MediatR;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request execution with timing and user context.
/// Handles uninitialized user context gracefully by logging appropriate values.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var (userId, orgId, isAuthenticated) = GetUserContext();

        Log.Information(
            "[START] {RequestName} | User: {UserId} | Org: {OrgId} | Authenticated: {IsAuthenticated}",
            requestName,
            userId,
            orgId,
            isAuthenticated);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next(cancellationToken);
            stopwatch.Stop();

            // Log success with result type info
            var resultInfo = GetResultInfo(response);
            Log.Information(
                "[END] {RequestName} | Duration: {Duration}ms | Result: {ResultInfo}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                resultInfo);

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Error(
                ex,
                "[FAILED] {RequestName} | Duration: {Duration}ms | User: {UserId} | Error: {ErrorMessage}",
                requestName,
                stopwatch.ElapsedMilliseconds,
                userId,
                ex.Message);

            throw;
        }
    }

    private (string UserId, string OrgId, bool IsAuthenticated) GetUserContext()
    {
        // Handle case where user context may not be initialized yet
        // This can happen if the behavior runs before UserContextMiddleware
        var isInitialized = currentUser is ICurrentUserInitializer initializer
            ? initializer.IsInitialized
            : true;

        if (!isInitialized)
        {
            return ("not-initialized", "not-initialized", false);
        }

        if (!currentUser.IsAuthenticated)
        {
            return ("anonymous", "none", false);
        }

        var userId = currentUser.UserId != Guid.Empty
            ? currentUser.UserId.ToString()
            : currentUser.ExternalUserId ?? "unknown";

        var orgId = currentUser.OrganizationId != Guid.Empty
            ? currentUser.OrganizationId.ToString()
            : currentUser.ExternalOrganizationId ?? "none";

        return (userId, orgId, true);
    }

    private static string GetResultInfo(TResponse response)
    {
        return response switch
        {
            Result { IsSuccess: true } => "Success",
            Result { IsFailure: true } r => $"Failure: {r.Error.Code}",
            null => "Null",
            _ => response.GetType().Name
        };
    }
}
