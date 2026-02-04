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
    private bool _isInitialized;

    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? ExternalUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                                     ?? User?.FindFirstValue("sub");

    public Guid UserId => _userId;
    public string PublicUserId => _publicUserId;
    public string? Email => User?.FindFirstValue("email");
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
    public Guid OrganizationId => _organizationId;
    public bool IsInitialized => _isInitialized;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized) return;

        if (!IsAuthenticated)
        {
            _isInitialized = true;
            return;
        }

        var externalUserId = ExternalUserId;
        var externalOrgId = User?.FindFirstValue("org_id");

        // Resolve IDs in parallel for better performance
        var userIdTask = !string.IsNullOrEmpty(externalUserId)
            ? idResolver.ResolveUserIdAsync(externalUserId, cancellationToken)
            : Task.FromResult(Guid.Empty);

        var publicUserIdTask = !string.IsNullOrEmpty(externalUserId)
            ? idResolver.ResolvePublicUserIdAsync(externalUserId, cancellationToken)
            : Task.FromResult(string.Empty);

        var orgIdTask = !string.IsNullOrEmpty(externalOrgId)
            ? idResolver.ResolveOrganizationIdAsync(externalOrgId, cancellationToken)
            : Task.FromResult(Guid.Empty);

        await Task.WhenAll(userIdTask, publicUserIdTask, orgIdTask);

        _userId = await userIdTask;
        _publicUserId = await publicUserIdTask;
        _organizationId = await orgIdTask;
        _isInitialized = true;
    }
}
