using MediatR;
using Microsoft.Extensions.Logging;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger,
    ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation(
            "[START] {RequestName} | User: {UserId} | Org: {OrgId}",
            requestName, currentUser.UserId, currentUser.OrganizationId);

        var stopwatch = Stopwatch.StartNew();
        var response = await next();
        stopwatch.Stop();

        logger.LogInformation(
            "[END] {RequestName} | Duration: {Duration}ms",
            requestName, stopwatch.ElapsedMilliseconds);

        return response;
    }
}
