using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Propentro.Infrastructure.Authorization;

public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService) : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        var endpoint = (context.Resource as HttpContext)?.GetEndpoint();
        var permissionAttributes = endpoint?.Metadata
            .GetOrderedMetadata<RequirePermissionAttribute>()
            .ToList();

        if (permissionAttributes is null || permissionAttributes.Count == 0)
        {
            // No permission requirements, succeed all pending requirements
            foreach (var requirement in context.PendingRequirements)
            {
                context.Succeed(requirement);
            }
            return;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationId = context.User.FindFirstValue("org_id");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(organizationId))
        {
            context.Fail(new AuthorizationFailureReason(this, "Missing authentication context"));
            return;
        }

        if (!Guid.TryParse(userId, out var userGuid) || !Guid.TryParse(organizationId, out var orgGuid))
        {
            context.Fail(new AuthorizationFailureReason(this, "Invalid user or organization ID"));
            return;
        }

        var userPermissions = await permissionService.GetUserPermissionsAsync(userGuid, orgGuid);

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
                context.Fail(new AuthorizationFailureReason(this,
                    $"Missing permission(s): {string.Join(", ", attr.Permissions)}"));
                return;
            }
        }

        foreach (var requirement in context.PendingRequirements)
        {
            context.Succeed(requirement);
        }
    }
}
