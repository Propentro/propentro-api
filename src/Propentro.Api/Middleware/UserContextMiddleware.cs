using Serilog;
using Serilog.Context;

namespace Propentro.Api.Middleware;

/// <summary>
/// Resolves authenticated users' external IDs to internal database IDs and pushes
/// user/organization context into Serilog LogContext for structured logging.
///
/// Runs BEFORE UseAuthorization() so that PermissionAuthorizationHandler can use
/// the already-initialized CurrentUser without needing its own initialization logic.
///
/// This middleware does NOT reject requests — that is the authorization layer's job.
/// </summary>
public sealed class UserContextMiddleware(RequestDelegate next)
{
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
            await next(context);
        }
    }
}
