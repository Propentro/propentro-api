using MediatR;

namespace Propentro.Domain.Common.Interfaces;

public interface IDomainEvent : INotification
{
    DateTime OccurredAt { get; }
}
