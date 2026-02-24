using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.UpdateContact;

public sealed class ContactUpdatedEventHandler : IOutboxEventHandler<ContactUpdatedEvent>
{
    public Task HandleAsync(ContactUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Log.Information("Handling {EventType} for contact {ContactId}", domainEvent.EventType, domainEvent.ContactId);

        // TODO: Sync changes to external systems, send webhook, etc.

        return Task.CompletedTask;
    }
}
