using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record LeaseCreatedEvent(Guid LeaseId, Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
