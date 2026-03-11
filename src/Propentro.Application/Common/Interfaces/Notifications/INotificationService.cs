namespace Propentro.Application.Common.Interfaces.Notifications;

/// <summary>
/// Service for creating notifications for organization members.
/// Used by outbox event handlers for async notification delivery.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Creates a notification for all admin users in the specified organization.
    /// </summary>
    Task CreateForOrgAdminsAsync(
        Guid organizationId,
        string title,
        string body,
        string category,
        string severity,
        string? entityType,
        string? entityId,
        Guid? senderId,
        CancellationToken ct);
}
