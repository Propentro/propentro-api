using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record MemberDeactivatedEvent(Guid AggregateId, Guid OrganizationId)
    : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "member.deactivated";
}
