using Dapper;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Lookups;

namespace Propentro.Application.Features.Lookups.Queries.GetCountries;

public sealed class GetCountriesHandler(
    IDbConnectionFactory db) : IQueryHandler<GetCountriesQuery, IReadOnlyList<CountryResponse>>
{
    public async Task<Result<IReadOnlyList<CountryResponse>>> Handle(
        GetCountriesQuery request,
        CancellationToken cancellationToken)
    {
        await using var connection = db.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        const string sql = """
            SELECT
                code,
                name,
                dial_code,
                currency_code,
                region_label
            FROM countries
            WHERE is_active = true
            ORDER BY name
            """;

        var countries = (await connection.QueryAsync<CountryQueryResult>(sql))
            .Select(c => new CountryResponse(c.Code, c.Name, c.DialCode, c.CurrencyCode, c.RegionLabel))
            .ToList();

        return countries;
    }

    private sealed class CountryQueryResult
    {
        public string Code { get; init; } = null!;
        public string Name { get; init; } = null!;
        public string? DialCode { get; init; }
        public string? CurrencyCode { get; init; }
        public string? RegionLabel { get; init; }
    }
}
