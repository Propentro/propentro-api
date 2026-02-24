namespace Propentro.Domain.Common.Interfaces;

public interface IDomainEvent
{
    Guid AggregateId { get; }
    Guid OrganizationId { get; }
    DateTimeOffset OccurredAt { get; }
}
