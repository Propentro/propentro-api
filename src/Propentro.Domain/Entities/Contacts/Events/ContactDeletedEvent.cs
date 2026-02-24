using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Contacts.Events;

public record ContactDeletedEvent(Guid ContactId, Guid OrganizationId)
    : DomainEvent(ContactId, OrganizationId);
