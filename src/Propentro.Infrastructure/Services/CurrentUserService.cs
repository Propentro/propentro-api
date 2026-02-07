using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.Http;
using Serilog;

namespace Propentro.Infrastructure.Services;

/// <summary>
/// Provides access to the current authenticated user's context.
/// Orchestrates identity resolution and organization membership verification.
/// </summary>
public sealed class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IExternalIdResolver idResolver,
    IDbConnectionFactory db) : ICurrentUser, ICurrentUserInitializer
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

        // Step 1: Resolve external IDs to internal IDs (auth provider specific)
        var identity = await idResolver.ResolveIdentityAsync(
            ExternalUserId,
            ExternalOrganizationId,
            cancellationToken);

        _userId = identity.UserId;
        _publicUserId = identity.PublicUserId;

        // Step 2: Verify user-org membership (auth provider agnostic)
        if (identity.UserId != Guid.Empty && identity.OrganizationId != Guid.Empty)
        {
            var isMember = await VerifyOrganizationMembershipAsync(
                identity.UserId, identity.OrganizationId, cancellationToken);

            if (isMember)
            {
                _organizationId = identity.OrganizationId;
                _publicOrganizationId = identity.PublicOrganizationId;
            }
            else
            {
                Log.Warning(
                    "User {UserId} is not an active member of organization {OrganizationId}",
                    identity.UserId, identity.OrganizationId);
                // _organizationId stays Guid.Empty → ActiveUserAuthorizationHandler will reject
            }
        }

        _isInitialized = true;
    }

    private async Task<bool> VerifyOrganizationMembershipAsync(
        Guid userId, Guid organizationId, CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT 1
            FROM organization_users
            WHERE user_id = @UserId
              AND organization_id = @OrganizationId
              AND status = 'active'
            LIMIT 1
            """;

        var result = await connection.QueryFirstOrDefaultAsync<int?>(
            sql, new { UserId = userId, OrganizationId = organizationId });

        return result.HasValue;
    }
}
