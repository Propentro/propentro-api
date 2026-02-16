using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Queries.GetContactById;

public sealed record GetContactByIdQuery(string Id) : IQuery<ContactResponse>;
