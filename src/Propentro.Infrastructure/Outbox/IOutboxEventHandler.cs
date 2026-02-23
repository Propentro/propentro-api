namespace Propentro.Infrastructure.Outbox;

/// <summary>
/// Handler for processing outbox messages of a specific event type.
/// Implementations are resolved from DI by matching EventType.
/// </summary>
public interface IOutboxEventHandler
{
    /// <summary>
    /// The event type this handler processes (e.g., "contact.created").
    /// </summary>
    string EventType { get; }

    /// <summary>
    /// Processes the outbox message.
    /// </summary>
    Task HandleAsync(OutboxMessage message, CancellationToken ct);
}
