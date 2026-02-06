using Propentro.Domain.Common;
using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record LeaseActivatedEvent(Guid LeaseId, Guid OrganizationId) : IDomainEvent
{
    public DateTime OccurredAt { get; } = DomainClock.UtcNow;
}
