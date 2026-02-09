using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.MyProfile.Queries.GetMe;
using Propentro.Contracts.MyProfile;
using Scalar.AspNetCore;

namespace Propentro.Api.Endpoints;

internal static class MeEndpoints
{
    public static void MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/me")
            .WithTags("MyProfile")
            .RequireAuthorization();

        group.MapGet("/", GetMeAsync)
            .WithDescription("Get current authenticated user profile, roles, and permissions")
            .Produces<MeDto>()
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
