using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities.Identity;

public sealed class SystemAdministrator : Entity, IAuditableEntity, ISoftDeletable
{
    public const string PublicIdPrefix = "sad";

    public Guid UserId { get; set; }
    public string Role { get; set; } = default!;
    public string? Note { get; set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // ISoftDeletable
    public bool IsActive { get; set; } = true;

    // Navigation
    public User User { get; set; } = default!;
}
