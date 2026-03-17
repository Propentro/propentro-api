using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Users.Commands.DeactivateUser;
using Propentro.Application.Features.Admin.Users.Commands.DeleteUser;
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

        group.MapGet("/{id}", GetAsync)
            .WithName("AdminGetUser")
            .WithSummary("Get user detail");

        group.MapPatch("/{id}/deactivate", DeactivateAsync)
            .WithName("AdminDeactivateUser")
            .WithSummary("Deactivate user");

        group.MapPatch("/{id}/reactivate", ReactivateAsync)
            .WithName("AdminReactivateUser")
            .WithSummary("Reactivate user");

        group.MapDelete("/{id}", DeleteAsync)
            .WithName("AdminDeleteUser")
            .WithSummary("Hard delete user (GDPR, irreversible)");
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
        string id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetUserQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateAsync(
        string id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new DeactivateUserCommand(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateAsync(
        string id, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ReactivateUserCommand(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteAsync(
        string id,
        HttpRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        if (!request.Headers.TryGetValue("X-Confirm-Delete", out var confirmHeader) ||
            !string.Equals(confirmHeader, "true", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Confirmation Required",
                detail: "Include the 'X-Confirm-Delete: true' header to confirm this irreversible operation.");
        }

        var result = await sender.Send(new DeleteUserCommand(id), cancellationToken);
        return result.ToHttpResult();
    }
}
