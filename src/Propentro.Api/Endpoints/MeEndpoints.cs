using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Me.Queries.GetMe;
using Propentro.Contracts.Me;

namespace Propentro.Api.Endpoints;

public static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me")
            .WithTags("Me")
            .RequireAuthorization();

        group.MapGet("/", GetMe)
            .WithName("GetMe")
            .WithSummary("Get current authenticated user profile")
            .Produces<MeDto>()
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetMe(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetMeQuery(), ct);
        return result.ToHttpResult();
    }
}
