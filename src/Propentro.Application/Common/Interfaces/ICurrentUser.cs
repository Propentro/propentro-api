namespace Propentro.Application.Common.Interfaces;

public interface ICurrentUser
{
    /// <summary>
    /// Internal Organization ID (DB): Guid
    /// </summary>
    Guid OrganizationId { get; }    

    /// <summary>
    /// Internal User ID (DB): Guid
    /// </summary>
    Guid UserId { get; }

    /// <summary>
    /// Internal Public ID (DB): String
    /// </summary>
    string PublicUserId { get; }

    /// <summary>
    /// Clerk User ID (external): "user_xxx"
    /// </summary>
    string? ExternalUserId { get; }

    /// <summary>
    /// User email from JWT
    /// </summary>
    string Email { get; }

    bool IsAuthenticated { get; }
}
