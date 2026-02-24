using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public record UserCreatedEvent(Guid UserId, Guid OrganizationId)
    : DomainEvent(UserId, OrganizationId)
{
    public override string EventType => "user.created";
}
