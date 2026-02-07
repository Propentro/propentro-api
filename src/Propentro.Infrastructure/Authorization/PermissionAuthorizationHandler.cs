using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Authorization handler that checks endpoint-level permission requirements.
/// Depends on UserContextMiddleware having already resolved and validated user context
/// (runs before UseAuthorization in the pipeline).
///
/// Responsibilities:
/// - No permission attributes → return early (let default handlers evaluate DenyAnonymous etc.)
/// - Has permission attributes → check user has required permissions, succeed or fail
/// </summary>
public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var httpContext = context.Resource as HttpContext;
        var endpoint = httpContext?.GetEndpoint();
        var permissionAttributes = endpoint?.Metadata
            .GetOrderedMetadata<RequirePermissionAttribute>()
            .ToList();

        // No permission requirements on this endpoint - let the default authorization
        // handlers (e.g. DenyAnonymousAuthorizationHandler) evaluate their own requirements.
        if (permissionAttributes is null || permissionAttributes.Count == 0)
        {
            return;
        }

        // CurrentUser is already initialized and validated by UserContextMiddleware.
        // If we reach here, the user must be authenticated with valid IDs.
        var currentUser = httpContext?.RequestServices.GetRequiredService<ICurrentUser>();
        if (currentUser is null || !currentUser.IsAuthenticated
            || currentUser.UserId == Guid.Empty || currentUser.OrganizationId == Guid.Empty)
        {
            context.Fail(new AuthorizationFailureReason(this, "User context not available"));
            return;
        }

        // Get user's permissions from cache/database
        var userPermissions = await permissionService.GetUserPermissionsAsync(
            currentUser.UserId,
            currentUser.OrganizationId,
            httpContext!.RequestAborted);

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

        // All permission checks passed
        foreach (var requirement in context.PendingRequirements)
        {
            context.Succeed(requirement);
        }
    }
}
