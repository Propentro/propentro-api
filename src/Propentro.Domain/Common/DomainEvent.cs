using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Common;

public abstract record DomainEvent(Guid AggregateId, Guid OrganizationId) : IDomainEvent
{
    public abstract string EventType { get; }
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
