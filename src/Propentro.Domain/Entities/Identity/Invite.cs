using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity;

public sealed class Invite : Entity
{
    public const string PublicIdPrefix = "inv";

    public static class Statuses
    {
        public const string Pending = "pending";
        public const string Accepted = "accepted";
        public const string Canceled = "canceled";
    }

    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = default!;
    public Guid RoleId { get; set; }
    public string Status { get; set; } = Statuses.Pending;
    public Guid? InvitedById { get; set; }
    public Guid? CanceledById { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    // Navigation — does NOT implement IAuditableEntity since it uses
    // InvitedById/CanceledById instead of standard CreatedById/UpdatedById
    public Organization Organization { get; set; } = default!;
    public Role Role { get; set; } = default!;
    public User? InvitedByUser { get; set; }
    public User? CanceledByUser { get; set; }

    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTimeOffset.UtcNow;
    public bool IsPendingAndValid => Status == Statuses.Pending && !IsExpired;
}
