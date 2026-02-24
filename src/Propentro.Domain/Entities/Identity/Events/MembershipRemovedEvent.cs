using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public record MembershipRemovedEvent(Guid UserId, Guid OrganizationId)
    : DomainEvent(UserId, OrganizationId)
{
    public override string EventType => "membership.removed";
}
