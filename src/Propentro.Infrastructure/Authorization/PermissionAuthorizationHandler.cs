using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Global authorization handler that runs for every authorized endpoint.
/// Depends on UserContextMiddleware having already initialized CurrentUser
/// (middleware runs before UseAuthorization in the pipeline).
///
/// Flow:
/// 1. Not authenticated → return early, let DenyAnonymousAuthorizationHandler handle it
/// 2. Authenticated → validate user and organization exist in the database
/// 3. If endpoint has RequirePermissionAttribute → check user has required permissions
/// 4. All checks passed → succeed all pending requirements
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var httpContext = context.Resource as HttpContext;

        // Not authenticated — don't interfere.
        // DenyAnonymousAuthorizationHandler will reject if endpoint requires auth.
        if (httpContext?.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        // Authenticated — validate user context is fully resolved.
        // CurrentUser is already initialized by UserContextMiddleware (runs before authorization).
        var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUser>();

        if (currentUser.UserId == Guid.Empty)
        {
            Log.Warning(
                "Authenticated user not found in database. ExternalUserId: {ExternalUserId}",
                currentUser.ExternalUserId);
            context.Fail(new AuthorizationFailureReason(this, "User not found in system"));
            return;
        }

        if (currentUser.OrganizationId == Guid.Empty)
        {
            Log.Warning(
                "Organization not resolved. ExternalUserId: {ExternalUserId}, ExternalOrgId: {ExternalOrgId}",
                currentUser.ExternalUserId,
                currentUser.ExternalOrganizationId);
            context.Fail(new AuthorizationFailureReason(this, "Organization context not available"));
            return;
        }

        // Check endpoint-level permission requirements (if any)
        var endpoint = httpContext.GetEndpoint();
        var permissionAttributes = endpoint?.Metadata
            .GetOrderedMetadata<RequirePermissionAttribute>()
            .ToList();

        if (permissionAttributes is { Count: > 0 })
        {
            var userPermissions = await permissionService.GetUserPermissionsAsync(
                currentUser.UserId,
                currentUser.OrganizationId,
                httpContext.RequestAborted);

            foreach (var attr in permissionAttributes)
            {
                var hasPermission = attr.Logic switch
                {
                    PermissionLogic.Any => attr.Permissions.Any(userPermissions.Contains),
                    PermissionLogic.All => attr.Permissions.All(userPermissions.Contains),
                    _ => false
                };

                if (!hasPermission)
                {
                    Log.Debug(
                        "Permission denied for user {UserId}. Required: {RequiredPermissions}, Has: {UserPermissions}",
                        currentUser.UserId,
                        attr.Permissions,
                        userPermissions);

                    context.Fail(new AuthorizationFailureReason(this,
                        $"Missing permission(s): {string.Join(", ", attr.Permissions)}"));
                    return;
                }
            }
        }

        // User is authenticated, exists in DB, has org context, and has required permissions.
        // Safe to succeed all pending requirements (including DenyAnonymous — user IS authenticated).
        foreach (var requirement in context.PendingRequirements)
        {
            context.Succeed(requirement);
        }
    }
}
