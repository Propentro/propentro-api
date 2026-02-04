namespace Propentro.Application.Common.Interfaces.Identity;

/// <summary>
/// Result of batch identity resolution.
/// </summary>
/// <param name="UserId">Internal user ID, or Guid.Empty if not found.</param>
/// <param name="PublicUserId">User's public ID, or empty string if not found.</param>
/// <param name="OrganizationId">Internal organization ID, or Guid.Empty if not found.</param>
/// <param name="PublicOrganizationId">Organization's public ID, or empty string if not found.</param>
public readonly record struct ResolvedIdentity(
    Guid UserId,
    string PublicUserId,
    Guid OrganizationId,
    string PublicOrganizationId);

/// <summary>
/// Resolves external authentication provider IDs to internal database IDs.
/// This abstraction allows for multiple auth providers (Clerk, Auth0, etc.).
/// </summary>
public interface IExternalIdResolver
{
    /// <summary>
    /// Resolves both user and organization external IDs in a single operation.
    /// This is more efficient than calling individual resolve methods when both are needed.
    /// </summary>
    /// <param name="externalUserId">The external user ID (e.g., "user_xxx" from Clerk).</param>
    /// <param name="externalOrgId">The external organization ID (e.g., "org_xxx" from Clerk).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved identity containing userId, publicUserId, and organizationId.</returns>
    Task<ResolvedIdentity> ResolveIdentityAsync(
        string? externalUserId,
        string? externalOrgId,
        CancellationToken cancellationToken = default);
}
