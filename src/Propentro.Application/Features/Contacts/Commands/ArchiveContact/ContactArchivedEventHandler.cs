using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.ArchiveContact;

public sealed class ContactArchivedEventHandler : IOutboxEventHandler<ContactArchivedEvent>
{
    public Task HandleAsync(ContactArchivedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Log.Information("Handling {EventType} for contact {ContactId}", domainEvent.EventType, domainEvent.ContactId);

        // TODO: Notify relevant parties, update external systems, etc.

        return Task.CompletedTask;
    }
}
