using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public string? UserId => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);

    public Guid OrganizationId
    {
        get
        {
            var orgClaim = _httpContextAccessor.HttpContext?.User.FindFirstValue("org_id");
            return Guid.TryParse(orgClaim, out var orgId) ? orgId : Guid.Empty;
        }
    }

    public string? Email => _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Email);

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
