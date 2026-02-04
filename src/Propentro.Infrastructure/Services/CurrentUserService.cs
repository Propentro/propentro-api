using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Interfaces.Identity;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Provides access to the current authenticated user's context.
/// Implements ICurrentUserInitializer for async initialization via middleware.
/// </summary>
/// <remarks>
/// This service is scoped per-request. The InitializeAsync method should be called
/// once per request by UserContextMiddleware to resolve external IDs to internal IDs.
/// After initialization, all property accesses are synchronous and fast.
/// </remarks>
public sealed class CurrentUserService : ICurrentUser, ICurrentUserInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IExternalIdResolver _idResolver;

    private Guid _userId = Guid.Empty;
    private Guid _organizationId = Guid.Empty;
    private string _publicUserId = string.Empty;
    private bool _isInitialized;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, IExternalIdResolver idResolver)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        _idResolver = idResolver ?? throw new ArgumentNullException(nameof(idResolver));
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    /// <inheritdoc />
    public string? ExternalUserId => User?.FindFirstValue(ClaimTypes.NameIdentifier)
                                     ?? User?.FindFirstValue("sub");

    /// <inheritdoc />
    public Guid UserId => _userId;

    /// <inheritdoc />
    public string PublicUserId => _publicUserId;

    /// <inheritdoc />
    public string? Email => User?.FindFirstValue("email");

    /// <inheritdoc />
    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    /// <inheritdoc />
    public Guid OrganizationId => _organizationId;

    /// <inheritdoc />
    public bool IsInitialized => _isInitialized;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_isInitialized)
        {
            return;
        }

        if (!IsAuthenticated)
        {
            _isInitialized = true;
            return;
        }

        var externalUserId = ExternalUserId;
        var externalOrgId = User?.FindFirstValue("org_id");

        // Resolve IDs in parallel for better performance
        var userIdTask = !string.IsNullOrEmpty(externalUserId)
            ? _idResolver.ResolveUserIdAsync(externalUserId, cancellationToken)
            : Task.FromResult(Guid.Empty);

        var publicUserIdTask = !string.IsNullOrEmpty(externalUserId)
            ? _idResolver.ResolvePublicUserIdAsync(externalUserId, cancellationToken)
            : Task.FromResult(string.Empty);

        var orgIdTask = !string.IsNullOrEmpty(externalOrgId)
            ? _idResolver.ResolveOrganizationIdAsync(externalOrgId, cancellationToken)
            : Task.FromResult(Guid.Empty);

        await Task.WhenAll(userIdTask, publicUserIdTask, orgIdTask);

        _userId = await userIdTask;
        _publicUserId = await publicUserIdTask;
        _organizationId = await orgIdTask;
        _isInitialized = true;
    }
}
