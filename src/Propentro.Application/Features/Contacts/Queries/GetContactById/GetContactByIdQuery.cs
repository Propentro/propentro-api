using Propentro.Contracts.Contacts;

namespace Propentro.Application.Features.Contacts.Queries.GetContactById;

public record GetContactByIdQuery(string Id) : IQuery<ContactResponse>, ICachedQuery<ContactResponse>
{
    public string CacheKey => $"contacts:{Id}";
    public TimeSpan? Expiration => TimeSpan.FromMinutes(10);
}
