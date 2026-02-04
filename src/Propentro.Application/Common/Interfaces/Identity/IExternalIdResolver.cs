namespace Propentro.Application.Common.Interfaces.Identity;

/// <summary>
/// Resolves external authentication provider IDs to internal database IDs.
/// This abstraction allows for multiple auth providers (Clerk, Auth0, etc.).
/// </summary>
public interface IExternalIdResolver
{
    /// <summary>
    /// Resolves an external organization ID to internal database organization ID.
    /// </summary>
    /// <param name="externalOrgId">The external organization ID (e.g., "org_xxx" from Clerk).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The internal organization ID, or Guid.Empty if not found.</returns>
    Task<Guid> ResolveOrganizationIdAsync(string externalOrgId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an external user ID to internal database user ID.
    /// </summary>
    /// <param name="externalUserId">The external user ID (e.g., "user_xxx" from Clerk).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The internal user ID, or Guid.Empty if not found.</returns>
    Task<Guid> ResolveUserIdAsync(string externalUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves an external user ID to the user's public identifier.
    /// </summary>
    /// <param name="externalUserId">The external user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's public ID, or empty string if not found.</returns>
    Task<string> ResolvePublicUserIdAsync(string externalUserId, CancellationToken cancellationToken = default);
}
