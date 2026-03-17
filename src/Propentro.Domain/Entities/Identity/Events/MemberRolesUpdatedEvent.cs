using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record MemberRolesUpdatedEvent(Guid AggregateId, Guid OrganizationId)
    : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "member.roles_updated";
}
