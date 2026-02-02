using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record PropertyCreatedEvent(Guid PropertyId, Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
