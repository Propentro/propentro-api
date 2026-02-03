using System.Diagnostics;
using MediatR;
using Propentro.Application.Common.Interfaces;
using Serilog;

namespace Propentro.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        Log.Information(
            "[START] {RequestName} | User: {UserId} | Org: {OrgId}",
            requestName, currentUser.UserId, currentUser.OrganizationId);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        Log.Information(
            "[END] {RequestName} | Duration: {Duration}ms",
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
