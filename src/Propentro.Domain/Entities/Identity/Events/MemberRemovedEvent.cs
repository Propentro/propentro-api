using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record MemberRemovedEvent(Guid AggregateId, Guid OrganizationId)
    : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "member.removed";
}
