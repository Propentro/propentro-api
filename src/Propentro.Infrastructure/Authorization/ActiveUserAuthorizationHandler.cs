using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Application.Common.Interfaces.Authentication;
using Serilog;

namespace Propentro.Infrastructure.Authorization;

/// <summary>
/// Handles <see cref="ActiveUserRequirement"/> by verifying that the current user
/// has been fully resolved to internal database IDs (user exists, org exists,
/// user is active member of org). This runs AFTER UserContextMiddleware.
/// </summary>
public sealed class ActiveUserAuthorizationHandler : AuthorizationHandler<ActiveUserRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveUserRequirement requirement)
    {
        if (context.Resource is not HttpContext httpContext)
            return Task.CompletedTask;

        var currentUser = httpContext.RequestServices.GetService<IUserContext>();
        if (currentUser is null)
        {
            context.Fail(new AuthorizationFailureReason(this, "User context service not available"));
            return Task.CompletedTask;
        }

        if (!currentUser.IsAuthenticated)
            return Task.CompletedTask; // Let DenyAnonymousAuthorizationRequirement handle this

        if (currentUser.UserId == Guid.Empty)
        {
            Log.Warning(
                "Authenticated user not found in system. ExternalUserId: {ExternalUserId}",
                currentUser.ExternalUserId);
            context.Fail(new AuthorizationFailureReason(this, "User not found in system"));
            return Task.CompletedTask;
        }

        // Skip org check for admin endpoints — SystemAdminAuthorizationHandler handles those
        var endpoint = httpContext.GetEndpoint();
        var requiresAdmin = endpoint?.Metadata.GetMetadata<RequireSystemAdministratorAttribute>();
        if (requiresAdmin is not null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (currentUser.OrganizationId == Guid.Empty)
        {
            Log.Warning(
                "Organization context not available for user {UserId}. ExternalOrgId: {ExternalOrgId}",
                currentUser.UserId,
                currentUser.ExternalOrganizationId);
            context.Fail(new AuthorizationFailureReason(this, "Organization context not available"));
            return Task.CompletedTask;
        }

        context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
