using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities;

public sealed class OrganizationUser : IAuditableEntity
{
    public Guid OrganizationId { get; set; }
    public Guid UserId { get; set; }
    public Guid? PartyId { get; set; }
    public string Status { get; set; } = "active";

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // Navigation
    public Organization Organization { get; set; } = default!;
    public User User { get; set; } = default!;
    public Party? Party { get; set; }
}
