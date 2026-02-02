using System.Security.Claims;
using Serilog.Context;

namespace Propentro.Api.Middleware;

/// <summary>
/// Pushes OrganizationId and UserId into Serilog LogContext
/// so that every log entry (even DB exceptions) is tagged with tenant context
/// </summary>
public sealed class UserContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var organizationId = context.User.FindFirstValue("organization_id");

        using (LogContext.PushProperty("UserId", userId ?? "anonymous"))
        using (LogContext.PushProperty("OrganizationId", organizationId ?? "none"))
        {
            await next(context);
        }
    }
}
