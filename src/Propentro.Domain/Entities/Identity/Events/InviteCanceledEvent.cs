using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Identity.Events;

public sealed record InviteCanceledEvent(Guid AggregateId, Guid OrganizationId)
    : DomainEvent(AggregateId, OrganizationId)
{
    public override string EventType => "invite.canceled";
}
