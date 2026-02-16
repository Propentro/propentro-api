using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Entities;

public sealed class User : Entity, IAuditableEntity, ISoftDeletable
{
    public string Email { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    // Auth Provider (Clerk, Entra, etc.)
    public string? ExternalId { get; set; }
    public string? AuthProvider { get; set; }

    // IAuditableEntity
    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    // ISoftDeletable
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<OrganizationUser> OrganizationUsers { get; set; } = [];
}
