using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Authorization handler that validates endpoint-specific permissions.
/// Acts as a gate: can only BLOCK (Fail) requests, never succeeds requirements.
/// Requirement success is handled by <see cref="ActiveUserAuthorizationHandler"/>.
///
/// Depends on UserContextMiddleware having already resolved CurrentUser
/// and ActiveUserAuthorizationHandler having verified user/org are valid.
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        // If authorization has already failed (e.g., user not active), skip permission check
        if (context.HasFailed)
            return;

        var httpContext = context.Resource as HttpContext;
        var endpoint = httpContext?.GetEndpoint();
        var permissionAttributes = endpoint?.Metadata
            .GetOrderedMetadata<RequirePermissionAttribute>()
            .ToList();

        // No permission requirements on this endpoint — nothing to check
        if (permissionAttributes is null || permissionAttributes.Count == 0)
            return;

        // If we don't have an HttpContext (e.g., non-HTTP resource), skip permission check
        if (httpContext is null)
            return;

        // At this point ActiveUserAuthorizationHandler has already verified
        // that CurrentUser is authenticated with valid UserId and OrganizationId
        var currentUser = httpContext.RequestServices.GetRequiredService<ICurrentUser>();

        // Get user's permissions from cache/database
        var userPermissions = await permissionService.GetUserPermissionsAsync(
            currentUser.UserId,
            currentUser.OrganizationId,
            httpContext.RequestAborted);

        // Check each required permission
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

        // Permissions passed — no need to succeed any requirement here.
        // ActiveUserAuthorizationHandler handles requirement success.
    }
}
