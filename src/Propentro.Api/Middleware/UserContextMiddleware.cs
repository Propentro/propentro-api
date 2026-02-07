using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;

namespace Propentro.Api.Middleware;

/// <summary>
/// Resolves authenticated users' external IDs to internal database IDs and validates
/// that the user and organization exist. Runs BEFORE authorization so that
/// PermissionAuthorizationHandler has a fully initialized CurrentUser context.
///
/// Flow:
/// 1. Not authenticated → skip validation, continue (authorization handles DenyAnonymous)
/// 2. Authenticated → resolve external IDs to internal IDs
/// 3. User not found in DB → short-circuit with 401
/// 4. Organization not resolved → short-circuit with 403
/// 5. All good → populate CurrentUser, enrich logs, continue to authorization
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

        // If the user has a valid JWT but is not found in our database,
        // reject early — no point reaching handlers or authorization checks.
        if (currentUser.IsAuthenticated)
        {
            if (currentUser.UserId == Guid.Empty)
            {
                Log.Warning(
                    "Authenticated user not found in database. ExternalUserId: {ExternalUserId}",
                    currentUser.ExternalUserId);

                await WriteAuthProblem(
                    context,
                    StatusCodes.Status401Unauthorized,
                    "User Not Found",
                    "Authenticated user does not exist in the system.",
                    "AUTH_USER_NOT_FOUND");
                return;
            }

            if (currentUser.OrganizationId == Guid.Empty)
            {
                Log.Warning(
                    "Organization not resolved for authenticated user. ExternalUserId: {ExternalUserId}, ExternalOrgId: {ExternalOrgId}",
                    currentUser.ExternalUserId,
                    currentUser.ExternalOrganizationId);

                await WriteAuthProblem(
                    context,
                    StatusCodes.Status403Forbidden,
                    "Organization Not Available",
                    "No organization context available for this user.",
                    "AUTH_ORG_NOT_FOUND");
                return;
            }
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

    private static async Task WriteAuthProblem(
        HttpContext context,
        int statusCode,
        string title,
        string detail,
        string code)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Type = $"https://propentro.com/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            Title = title,
            Status = statusCode,
            Detail = detail,
            Instance = traceId,
            Extensions =
            {
                ["code"] = code,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        });
    }
}
