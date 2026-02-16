using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record PropertyUpdatedEvent(Guid PropertyId, Guid OrganizationId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
