using Propentro.Contracts.Documents;

namespace Propentro.Application.Features.Contacts.Queries.GetContactDocuments;

public record GetContactDocumentsQuery(string ContactId) : IQuery<IReadOnlyList<ContactDocumentResponse>>;
