using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record OrganizationDeactivatedEvent(
    Guid AggregateId,
    Guid OrganizationId,
    IReadOnlyList<Guid> MemberUserIds) : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "organization.deactivated";
}
