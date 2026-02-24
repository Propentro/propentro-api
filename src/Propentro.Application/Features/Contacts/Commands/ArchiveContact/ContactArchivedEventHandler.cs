using System.Text.Json;
using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.ArchiveContact;

public sealed class ContactArchivedEventHandler : IOutboxEventHandler
{
    public string EventType => "contact.archived";

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<ContactArchivedEvent>(payload, OutboxJsonOptions.Instance);

        Log.Information("Handling {EventType} for contact {ContactId}", EventType, domainEvent!.ContactId);

        // TODO: Notify relevant parties, update external systems, etc.

        return Task.CompletedTask;
    }
}
