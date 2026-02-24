using Propentro.Domain.Common;

namespace Propentro.Domain.Entities.Contacts.Events;

public record ContactArchivedEvent(Guid ContactId, Guid OrganizationId)
    : DomainEvent(ContactId, OrganizationId);
