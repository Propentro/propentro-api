namespace Propentro.Application.Common.Interfaces;

public interface ICurrentUser
{
    /// <summary>
    /// Clerk User ID (external): "user_xxx"
    /// </summary>
    string? ExternalUserId { get; }

    /// <summary>
    /// Internal User ID (DB): Guid
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Internal Organization ID (DB): Guid
    /// </summary>
    Guid OrganizationId { get; }

    /// <summary>
    /// User email from JWT
    /// </summary>
    string? Email { get; }

    bool IsAuthenticated { get; }
}
