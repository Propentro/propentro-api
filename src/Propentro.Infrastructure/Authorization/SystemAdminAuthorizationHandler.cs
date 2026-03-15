using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Application.Common.Interfaces.Authentication;
using Serilog;

namespace Propentro.Infrastructure.Authorization;

public sealed class SystemAdminAuthorizationHandler : IAuthorizationHandler
{
    public async Task HandleAsync(AuthorizationHandlerContext context)
    {
        if (context.HasFailed)
            return;

        var httpContext = context.Resource as HttpContext;
        var endpoint = httpContext?.GetEndpoint();

        var requiresAdmin = endpoint?.Metadata
            .GetMetadata<RequireSystemAdministratorAttribute>();

        if (requiresAdmin is null)
            return;

        if (httpContext is null)
            return;

        var currentUser = httpContext.RequestServices.GetRequiredService<IUserContext>();

        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            context.Fail(new AuthorizationFailureReason(this, "Not authenticated"));
            return;
        }

        // Check 1: Clerk private metadata claim
        var systemRoleClaim = httpContext.User.FindFirst("system_role")?.Value
            ?? httpContext.User.FindFirst("metadata.systemRole")?.Value;

        if (systemRoleClaim != "super_admin")
        {
            Log.Debug("System admin check failed: missing or invalid system_role claim for user {UserId}", currentUser.UserId);
            context.Fail(new AuthorizationFailureReason(this, "Not a system administrator"));
            return;
        }

        // Check 2: DB record in system_administrators
        var dbContext = httpContext.RequestServices.GetRequiredService<IAppDbContext>();
        var isAdmin = await dbContext.SystemAdministrators
            .IgnoreQueryFilters()
            .AnyAsync(
                sa => sa.UserId == currentUser.UserId && sa.IsActive,
                httpContext.RequestAborted);

        if (!isAdmin)
        {
            Log.Warning("System admin check failed: no active DB record for user {UserId} despite valid claim", currentUser.UserId);
            context.Fail(new AuthorizationFailureReason(this, "Not a system administrator"));
            return;
        }

        Log.Debug("System admin access granted for user {UserId}", currentUser.UserId);
    }
}
