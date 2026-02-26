using Propentro.Application.Common.Models;
using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Queries.GetContacts;

public record GetContactsQuery(
    int Page,
    int PageSize,
    string? Role,
    string? Search) : IQuery<PagedResult<ContactListItemResponse>>, ICachedQuery<PagedResult<ContactListItemResponse>>
{
    public string CacheKey => $"contacts:list:{Page}:{PageSize}:{Role ?? ""}:{Search ?? ""}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(5);
}
