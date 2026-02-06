using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record PartyCreatedEvent(Guid PartyId, Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
