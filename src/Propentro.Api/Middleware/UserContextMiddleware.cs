using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Interfaces.Identity;
using Serilog.Context;

namespace Propentro.Api.Middleware;

/// <summary>
/// Initializes the current user context and pushes user/organization IDs into Serilog LogContext.
/// This middleware ensures that:
/// 1. External auth provider IDs are resolved to internal database IDs (async)
/// 2. Every log entry is tagged with tenant context for tracing
/// </summary>
public sealed class UserContextMiddleware
{
    private readonly RequestDelegate _next;

    public UserContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ICurrentUser currentUser)
    {
        // Initialize user context (resolves external IDs to internal IDs)
        if (currentUser is ICurrentUserInitializer initializer && !initializer.IsInitialized)
        {
            await initializer.InitializeAsync(context.RequestAborted);
        }

        // Push resolved IDs to log context for structured logging
        var userId = currentUser.IsAuthenticated ? currentUser.UserId.ToString() : "anonymous";
        var organizationId = currentUser.IsAuthenticated && currentUser.OrganizationId != Guid.Empty
            ? currentUser.OrganizationId.ToString()
            : "none";
        var externalUserId = currentUser.ExternalUserId ?? "none";

        using (LogContext.PushProperty("UserId", userId))
        using (LogContext.PushProperty("OrganizationId", organizationId))
        using (LogContext.PushProperty("ExternalUserId", externalUserId))
        {
            await _next(context);
        }
    }
}
