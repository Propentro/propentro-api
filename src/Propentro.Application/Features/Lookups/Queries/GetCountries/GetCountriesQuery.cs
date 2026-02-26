using Propentro.Contracts.Lookups;

namespace Propentro.Application.Features.Lookups.Queries.GetCountries;

public record GetCountriesQuery : IQuery<IReadOnlyList<CountryResponse>>, ICachedQuery<IReadOnlyList<CountryResponse>>
{
    public string CacheKey => "countries:all";
    public TimeSpan? Expiration => TimeSpan.FromHours(24);
}
