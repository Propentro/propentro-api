using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Me.Queries.GetMe;
using Propentro.Contracts.Me;
using Scalar.AspNetCore;

namespace Propentro.Api.Endpoints;

internal static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/me")
            .WithTags("Me")
            .RequireAuthorization();

        group.MapGet("/", GetMeAsync)
            .WithName("GetMe")
            .WithSummary("Get current user profile")
            .Produces<MeResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetMeAsync(
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetMeQuery(), ct);
        return result.ToHttpResult();
    }
}
