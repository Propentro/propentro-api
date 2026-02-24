using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Queries.GetContactById;

public record GetContactByIdQuery(string Id) : IQuery<ContactResponse>;
