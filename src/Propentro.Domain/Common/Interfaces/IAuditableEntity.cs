namespace Propentro.Domain.Common.Interfaces;

public interface IAuditableEntity
{
    DateTimeOffset CreatedAt { get; set; }
    Guid? CreatedById { get; set; }
    DateTimeOffset? UpdatedAt { get; set; }
    Guid? UpdatedById { get; set; }
}
