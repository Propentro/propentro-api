using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record LeaseActivatedEvent(Guid LeaseId, Guid OrganizationId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
