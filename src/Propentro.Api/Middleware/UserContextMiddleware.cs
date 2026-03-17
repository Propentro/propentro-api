using Propentro.Application.Common.Interfaces.Authentication;
using Serilog.Context;

namespace Propentro.Api.Middleware;

/// <summary>
/// Initializes the current user context and pushes user/organization IDs into Serilog LogContext.
/// This middleware ensures that:
/// 1. A correlation ID is established for request tracing
/// 2. External auth provider IDs are resolved to internal database IDs (async)
/// 3. Every log entry is tagged with tenant context for tracing
/// </summary>
public sealed class UserContextMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context, IUserContext userContext)
    {
        // Establish correlation ID for request tracing
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString();
        context.Response.Headers[CorrelationIdHeader] = correlationId;

        // Initialize user context (resolves external IDs to internal IDs)
        if (userContext is IUserContextInitializer initializer && !initializer.IsInitialized)
        {
            await initializer.InitializeAsync(context.RequestAborted);
        }

        // Push resolved IDs to log context for structured logging
        var userId = userContext.IsAuthenticated
            ? userContext.UserId.ToString()
            : "anonymous";
        var organizationId = userContext.IsAuthenticated && userContext.OrganizationId != Guid.Empty
            ? userContext.OrganizationId.ToString()
            : "none";
        var externalUserId = userContext.ExternalUserId ?? "none";

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("OrganizationId", organizationId))
        using (LogContext.PushProperty("ExternalUserId", externalUserId))
        {
            await next(context);
        }
    }
}
