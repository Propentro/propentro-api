using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Lookups.Queries.GetCountries;
using Propentro.Contracts.Lookups;

namespace Propentro.Api.Endpoints;

internal static class LookupEndpoints
{
    public static void MapLookupEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/lookups")
            .WithTags("Lookups")
            .RequireAuthorization();

        group.MapGet("/countries", GetCountriesAsync)
            .WithName("GetCountries")
            .WithSummary("List all countries")
            .Produces<IReadOnlyList<CountryResponse>>();
    }

    private static async Task<IResult> GetCountriesAsync(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetCountriesQuery(), ct);
        return result.ToHttpResult();
    }
}
