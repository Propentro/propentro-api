using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Properties.Events;

public record PropertyUpdatedEvent(Guid PropertyId, Guid OrganizationId)
    : DomainEvent(PropertyId, OrganizationId)
{
    public override string EventType => "property.updated";
}
