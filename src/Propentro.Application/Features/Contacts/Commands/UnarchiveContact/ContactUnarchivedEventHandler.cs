using System.Text.Json;
using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.UnarchiveContact;

public sealed class ContactUnarchivedEventHandler : IOutboxEventHandler
{
    public string EventType => "contact.unarchived";

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<ContactUnarchivedEvent>(payload, OutboxJsonOptions.Instance);

        Log.Information("Handling {EventType} for contact {ContactId}", EventType, domainEvent!.ContactId);

        // TODO: Re-activate in external systems, etc.

        return Task.CompletedTask;
    }
}
