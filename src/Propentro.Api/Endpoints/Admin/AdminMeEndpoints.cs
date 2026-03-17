using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Me.Queries.GetAdminMe;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminMeEndpoints
{
    public static void MapAdminMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin")
            .WithTags("Admin Profile")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/me", GetAdminMeAsync)
            .WithName("AdminGetMe")
            .WithSummary("Get admin profile");
    }

    private static async Task<IResult> GetAdminMeAsync(
        ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetAdminMeQuery(), ct);
        return result.ToHttpResult();
    }
}
