using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Propentro.Application.Common.Interfaces;

namespace Propentro.Infrastructure.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor, IClerkIdResolver clerkIdResolver) : ICurrentUser
{
    private Guid? _cachedUserId;
    private Guid? _cachedOrgId;

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? ExternalUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                                     ?? User?.FindFirstValue("sub");

    public string? Email => User?.FindFirstValue("email");

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    public Guid OrganizationId
    {
        get
        {
            if (_cachedOrgId.HasValue)
            {
                return _cachedOrgId.Value;
            }

            string? clerkOrgId = User?.FindFirstValue("org_id");

            if (string.IsNullOrEmpty(clerkOrgId))
            {
                return Guid.Empty;
            }

            _cachedOrgId = clerkIdResolver
                .ResolveOrganizationIdAsync(clerkOrgId)
                .GetAwaiter().GetResult();

            return _cachedOrgId.Value;
        }
    }

    public Guid UserId
    {
        get
        {
            if (_cachedUserId.HasValue)
            {
                return _cachedUserId.Value;
            }

            string? clerkUserId = ExternalUserId;

            if (string.IsNullOrEmpty(clerkUserId))
            {
                return Guid.Empty;
            }

            _cachedUserId = clerkIdResolver
                .ResolveUserIdAsync(clerkUserId)
                .GetAwaiter().GetResult();

            return _cachedUserId.Value;
        }
    }
}
