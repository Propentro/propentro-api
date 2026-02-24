using Propentro.Domain.Entities.Contacts.Events;

namespace Propentro.Application.Features.Contacts.Commands.CreateContact;

public sealed class ContactCreatedEventHandler : IOutboxEventHandler<ContactCreatedEvent>
{
    public Task HandleAsync(ContactCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        Log.Information("Handling {EventType} for contact {ContactId}", domainEvent.EventType, domainEvent.ContactId);

        // TODO: Send welcome email, notify team, etc.

        return Task.CompletedTask;
    }
}
