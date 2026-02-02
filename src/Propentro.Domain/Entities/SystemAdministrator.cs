using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities;

public sealed class SystemAdministrator : Entity, IAuditableEntity, ISoftDeletable
{
    public Guid UserId { get; set; }
    public string Role { get; set; } = default!;
    public string? Note { get; set; }

    // IAuditableEntity
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // ISoftDeletable
    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = default!;
}
