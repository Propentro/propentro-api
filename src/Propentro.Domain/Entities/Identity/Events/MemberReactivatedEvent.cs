using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record MemberReactivatedEvent(Guid AggregateId, Guid OrganizationId)
    : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "member.reactivated";
}
