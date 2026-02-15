using MediatR;

namespace Propentro.Domain.Common.Interfaces;

public interface IDomainEvent : INotification
{
    DateTimeOffset OccurredAt { get; }
}
