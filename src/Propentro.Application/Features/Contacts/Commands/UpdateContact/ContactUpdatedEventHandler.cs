using System.Text.Json;
using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.UpdateContact;

public sealed class ContactUpdatedEventHandler : IOutboxEventHandler
{
    public string EventType => "contact.updated";

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<ContactUpdatedEvent>(payload, OutboxJsonOptions.Instance);

        Log.Information("Handling {EventType} for contact {ContactId}", EventType, domainEvent!.ContactId);

        // TODO: Sync changes to external systems, send webhook, etc.

        return Task.CompletedTask;
    }
}
