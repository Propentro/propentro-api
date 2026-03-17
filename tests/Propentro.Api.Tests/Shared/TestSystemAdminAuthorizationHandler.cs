using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Tests.Shared;

/// <summary>
/// Test replacement for <see cref="SystemAdminAuthorizationHandler"/> that checks only the
/// database <c>system_administrators</c> record, bypassing the Clerk API call.
/// </summary>
internal sealed class TestSystemAdminAuthorizationHandler : IAuthorizationHandler
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

        var dbContext = httpContext.RequestServices.GetRequiredService<IAppDbContext>();
        var isAdmin = await dbContext.SystemAdministrators
            .IgnoreQueryFilters()
            .AnyAsync(
                sa => sa.UserId == currentUser.UserId && sa.IsActive,
                httpContext.RequestAborted);

        if (!isAdmin)
        {
            context.Fail(new AuthorizationFailureReason(this, "Not a system administrator"));
            return;
        }
    }
}
