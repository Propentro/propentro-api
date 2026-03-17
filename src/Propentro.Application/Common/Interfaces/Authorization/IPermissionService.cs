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
    Task<HashSet<string>> GetUserPermissionsAsync(
        Guid userId,
        Guid organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user has a specific permission within an organization.
    /// </summary>
    Task<bool> HasPermissionAsync(
        Guid userId,
        Guid organizationId,
        string permissionCode,
        CancellationToken cancellationToken = default);
}
