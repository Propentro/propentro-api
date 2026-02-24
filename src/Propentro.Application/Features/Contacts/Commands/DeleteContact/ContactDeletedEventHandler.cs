using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.DeleteContact;

public sealed class ContactDeletedEventHandler : IOutboxEventHandler<ContactDeletedEvent>
{
    public Task HandleAsync(ContactDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Log.Information("Handling {EventType} for contact {ContactId}", domainEvent.EventType, domainEvent.ContactId);

        // TODO: Remove from external systems, send webhook, etc.

        return Task.CompletedTask;
    }
}
