using Propentro.Domain.Common.Interfaces;

namespace Propentro.Application.Common.Messaging;

public abstract class OutboxEventHandler<TEvent> : IOutboxEventHandler
    where TEvent : IDomainEvent
{
    public abstract string EventType { get; }

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var @event = OutboxJsonOptions.Deserialize<TEvent>(payload);
        return HandleAsync(@event, cancellationToken);
    }

    protected abstract Task HandleAsync(TEvent @event, CancellationToken cancellationToken);
}
