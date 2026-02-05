using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Authorization handler that validates user permissions using the initialized ICurrentUser context.
/// This handler depends on UserContextMiddleware having already resolved external IDs to internal IDs.
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

        // No permission requirements - succeed all pending requirements
        if (permissionAttributes is null || permissionAttributes.Count == 0)
        {
            foreach (var requirement in context.PendingRequirements)
            {
                context.Succeed(requirement);
            }
            return;
        }

        // Get ICurrentUser from DI - must be already initialized by UserContextMiddleware
        var currentUser = httpContext?.RequestServices.GetService<ICurrentUser>();
        if (currentUser is null)
        {
            Log.Warning("ICurrentUser service not available in authorization handler");
            context.Fail(new AuthorizationFailureReason(this, "Authorization service unavailable"));
            return;
        }

        // Ensure user context is initialized (defensive check)
        if (currentUser is ICurrentUserInitializer { IsInitialized: false } initializer)
        {
            try
            {
                await initializer.InitializeAsync(httpContext!.RequestAborted);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to initialize user context in authorization handler");
                context.Fail(new AuthorizationFailureReason(this, "Failed to initialize user context"));
                return;
            }
        }

        // User must be authenticated
        if (!currentUser.IsAuthenticated)
        {
            context.Fail(new AuthorizationFailureReason(this, "User is not authenticated"));
            return;
        }

        // Internal IDs must be resolved (not Guid.Empty)
        if (currentUser.UserId == Guid.Empty)
        {
            Log.Warning(
                "User ID not resolved. ExternalUserId: {ExternalUserId}",
                currentUser.ExternalUserId);
            context.Fail(new AuthorizationFailureReason(this, "User not found in system"));
            return;
        }

        if (currentUser.OrganizationId == Guid.Empty)
        {
            Log.Warning(
                "Organization ID not resolved. ExternalOrgId: {ExternalOrgId}",
                currentUser.ExternalOrganizationId);
            context.Fail(new AuthorizationFailureReason(this, "Organization context not available"));
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
