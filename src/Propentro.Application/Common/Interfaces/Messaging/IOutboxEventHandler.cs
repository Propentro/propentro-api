using Propentro.Domain.Common.Interfaces;

namespace Propentro.Application.Common.Interfaces.Messaging;

public interface IOutboxEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
