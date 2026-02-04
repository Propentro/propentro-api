using MediatR;
using Serilog;

namespace Propentro.Application.Common.Behaviors;

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

        Log.Information(
            "[START] {RequestName} | User: {UserId} | Org: {OrgId}",
            requestName, currentUser.UserId, currentUser.OrganizationId);

        var stopwatch = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        stopwatch.Stop();

        Log.Information(
            "[END] {RequestName} | Duration: {Duration}ms",
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
