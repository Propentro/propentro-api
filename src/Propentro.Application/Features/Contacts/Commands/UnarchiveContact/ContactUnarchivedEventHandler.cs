using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.UnarchiveContact;

public sealed class ContactUnarchivedEventHandler : IOutboxEventHandler<ContactUnarchivedEvent>
{
    public Task HandleAsync(ContactUnarchivedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Log.Information("Handling {EventType} for contact {ContactId}", domainEvent.EventType, domainEvent.ContactId);

        // TODO: Re-activate in external systems, etc.

        return Task.CompletedTask;
    }
}
