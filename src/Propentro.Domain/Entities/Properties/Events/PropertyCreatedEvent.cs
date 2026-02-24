using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Properties.Events;

public record PropertyCreatedEvent(Guid PropertyId, Guid OrganizationId)
    : DomainEvent(PropertyId, OrganizationId)
{
    public override string EventType => "property.created";
}
