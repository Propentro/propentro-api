namespace Propentro.Application.Common.Interfaces.Messaging;

public interface IOutboxEventHandler
{
    string EventType { get; }
    Task HandleAsync(string payload, CancellationToken cancellationToken = default);
}
