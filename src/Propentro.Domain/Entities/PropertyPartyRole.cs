using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities;

public sealed class PropertyPartyRole : IOrganizationScoped, IAuditableEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid PartyId { get; set; }
    public string Role { get; set; } = default!;
    public bool IsPrimary { get; set; }
    public decimal? OwnershipPct { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? Note { get; set; }

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // Navigation
    public Property Property { get; set; } = default!;
    public Party Party { get; set; } = default!;
}
