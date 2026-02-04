namespace Propentro.Application.Common.Interfaces.Identity;

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
    /// Invalidates the cached permissions for a user within an organization.
    /// Call this when role assignments change.
    /// </summary>
    /// <param name="userId">The internal user ID.</param>
    /// <param name="organizationId">The internal organization ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task InvalidatePermissionsCacheAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
