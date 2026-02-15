using Propentro.Application.Common.Models;
using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Queries.GetContacts;

public sealed record GetContactsQuery(
    string? Role,
    string? Search,
    bool? IsArchived,
    int Page = 1,
    int PageSize = 25) : IQuery<PagedResult<ContactListItemResponse>>;
