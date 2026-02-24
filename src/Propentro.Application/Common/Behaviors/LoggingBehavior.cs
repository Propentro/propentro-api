using MediatR;
using Propentro.Application.Common.Interfaces.Authentication;

namespace Propentro.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs request execution with timing.
/// </summary>
public sealed class LoggingBehavior<TRequest, TResponse>(
    IUserContext userContext) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        Log.Information(
            "[START] {RequestName} | User: {UserId} | Org: {OrgId}",
            requestName,
            userContext.UserId,
            userContext.OrganizationId);

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        Log.Information(
            "[END] {RequestName} | Duration: {Duration}ms",
            requestName,
            stopwatch.ElapsedMilliseconds);

        return response;
    }
}
