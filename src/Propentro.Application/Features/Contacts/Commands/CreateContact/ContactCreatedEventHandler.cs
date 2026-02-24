using System.Text.Json;
using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public sealed class ContactCreatedEventHandler : IOutboxEventHandler
{
    public string EventType => "contact.created";

    public Task HandleAsync(string payload, CancellationToken cancellationToken = default)
    {
        var domainEvent = JsonSerializer.Deserialize<ContactCreatedEvent>(payload);

        Log.Information("Handling {EventType} for contact {ContactId}", EventType, domainEvent!.ContactId);

        // TODO: Send welcome email, notify team, etc.

        return Task.CompletedTask;
    }
}
