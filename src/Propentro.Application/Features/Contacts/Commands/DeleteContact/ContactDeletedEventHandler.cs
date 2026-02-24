using System.Text.Json;
using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.DeleteContact;

public sealed class ContactDeletedEventHandler : IOutboxEventHandler
{
    public string EventType => "contact.deleted";

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<ContactDeletedEvent>(payload);

        Log.Information("Handling {EventType} for contact {ContactId}", EventType, domainEvent!.ContactId);

        // TODO: Remove from external systems, send webhook, etc.

        return Task.CompletedTask;
    }
}
