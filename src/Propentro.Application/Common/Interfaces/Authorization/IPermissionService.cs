namespace Propentro.Application.Common.Interfaces.Authorization;

/// <summary>
/// Provides permission lookup for authorization purposes.
/// Permissions are cached per user/organization to minimize database queries.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Gets all permission codes assigned to a user within an organization.
    /// Results are cached for performance.
    /// </summary>
    /// <param name="userId">The internal user ID.</param>
    /// <param name="organizationId">The internal organization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A set of permission codes (e.g., "units:read", "properties:write").</returns>
    Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission within an organization.
    /// </summary>
    /// <param name="userId">The internal user ID.</param>
    /// <param name="organizationId">The internal organization ID.</param>
    /// <param name="permissionCode">The permission code to check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the user has the permission, false otherwise.</returns>
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permissionCode,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates the cached permissions for a specific user within an organization.
    /// Call this when a specific user's role assignment changes.
    /// </summary>
    /// <param name="userId">The internal user ID.</param>
    /// <param name="organizationId">The internal organization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidatePermissionsCacheAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidates all cached permissions for an organization.
    /// Call this when roles or permissions are modified at the organization level
    /// (e.g., a permission is added/removed from a role).
    /// </summary>
    /// <param name="organizationId">The internal organization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidateOrganizationPermissionsCacheAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
