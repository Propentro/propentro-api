using Propentro.Domain.Common.Interfaces;

namespace Propentro.Domain.Events;

public sealed record ContactCreatedEvent(Guid ContactId, Guid OrganizationId) : IDomainEvent
{
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
