using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Contacts.Events;

public record ContactUpdatedEvent(Guid ContactId, Guid OrganizationId)
    : DomainEvent(ContactId, OrganizationId);
