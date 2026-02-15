using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record ContactCreatedEvent(Guid ContactId, Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
