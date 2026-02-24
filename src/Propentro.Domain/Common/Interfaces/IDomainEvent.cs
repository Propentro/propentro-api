namespace Propentro.Domain.Common.Interfaces;

public interface IDomainEvent
{
    string EventType { get; }
    Guid AggregateId { get; }
    Guid OrganizationId { get; }
    DateTimeOffset OccurredAt { get; }
}
