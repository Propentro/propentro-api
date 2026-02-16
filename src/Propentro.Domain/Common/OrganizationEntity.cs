using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

public abstract class OrganizationEntity : Entity, IOrganizationScoped, IAuditableEntity, ISoftDeletable
{
    public Guid OrganizationId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }

    public bool IsActive { get; set; } = true;

    protected OrganizationEntity() { }

    protected OrganizationEntity(string prefix) : base(prefix) { }
}
