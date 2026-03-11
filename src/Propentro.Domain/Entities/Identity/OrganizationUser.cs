using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities.Identity;

public sealed class OrganizationUser : IAuditableEntity
{
    public static class Statuses
    {
        public const string Active = "active";
        public const string Deactivated = "deactivated";
    }

    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = Statuses.Active;

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // Navigation
    public Organization Organization { get; set; } = default!;
    public User User { get; set; } = default!;
}
