using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public sealed class PermissionAuthorizationHandler(
    IPermissionService permissionService) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var orgIdClaim = context.User.FindFirstValue("org_id");

        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(orgIdClaim))
        {
            return;
        }

        if (!Guid.TryParse(userIdClaim, out var userId) || !Guid.TryParse(orgIdClaim, out var orgId))
        {
            return;
        }

        var permissions = await permissionService.GetUserPermissionsAsync(userId, orgId);

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
