using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Provides access to the current authenticated user's context.
/// Implements ICurrentUserInitializer for async initialization via middleware.
/// </summary>
public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IExternalIdResolver idResolver) : ICurrentUser, ICurrentUserInitializer
{
    private Guid _userId = Guid.Empty;
    private Guid _organizationId = Guid.Empty;
    private string _publicUserId = string.Empty;
    private string _publicOrganizationId = string.Empty;
    private bool _isInitialized;

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? ExternalUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                                     ?? User?.FindFirstValue("sub");

    public string? ExternalOrganizationId => User?.FindFirstValue("org_id");

    public Guid UserId => _userId;
    public string PublicUserId => _publicUserId;
    public string? Email => User?.FindFirstValue("email");
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public Guid OrganizationId => _organizationId;
    public string PublicOrganizationId => _publicOrganizationId;
    public bool IsInitialized => _isInitialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        if (!IsAuthenticated)
        {
            _isInitialized = true;
            return;
        }

        // Single call resolves all IDs efficiently with one connection
        var identity = await idResolver.ResolveIdentityAsync(
            ExternalUserId,
            ExternalOrganizationId,
            cancellationToken);

        _userId = identity.UserId;
        _publicUserId = identity.PublicUserId;
        _organizationId = identity.OrganizationId;
        _publicOrganizationId = identity.PublicOrganizationId;
        _isInitialized = true;
    }
}
