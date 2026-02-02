using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public Guid OrganizationId
    {
        get
        {
            var orgClaim = httpContextAccessor.HttpContext?.User.FindFirstValue("org_id");
            return Guid.TryParse(orgClaim, out var orgId) ? orgId : Guid.Empty;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
