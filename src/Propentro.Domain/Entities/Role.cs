using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities;

public sealed class Role : Entity, IAuditableEntity, ISoftDeletable
{
    public Guid? OrganizationId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // ISoftDeletable
    public bool IsActive { get; set; } = true;

    // Navigation
    public Organization? Organization { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
