using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Users.Commands.DeactivateUser;
using Propentro.Application.Features.Admin.Users.Commands.ReactivateUser;
using Propentro.Application.Features.Admin.Users.Queries.GetUser;
using Propentro.Application.Features.Admin.Users.Queries.GetUsers;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/users")
            .WithTags("Admin Users")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/", ListAsync)
            .WithName("AdminListUsers")
            .WithSummary("List all users");

        group.MapGet("/{publicId}", GetAsync)
            .WithName("AdminGetUser")
            .WithSummary("Get user detail");

        group.MapPatch("/{publicId}/deactivate", DeactivateAsync)
            .WithName("AdminDeactivateUser")
            .WithSummary("Deactivate user");

        group.MapPatch("/{publicId}/reactivate", ReactivateAsync)
            .WithName("AdminReactivateUser")
            .WithSummary("Reactivate user");
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        string? search,
        bool? isActive,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetUsersQuery(search, isActive, page, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAsync(
        string publicId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserQuery(publicId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateAsync(
        string publicId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateUserCommand(publicId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateAsync(
        string publicId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReactivateUserCommand(publicId), cancellationToken);
        return result.ToHttpResult();
    }
}
