using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;
using Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;
using Propentro.Application.Features.Admin.Users.Commands.RemoveMember;
using Propentro.Application.Features.Admin.Users.Queries.GetOrganizationMembers;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminMemberEndpoints
{
    public static void MapAdminMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/organizations/{organizationId}/members")
            .WithTags("Admin Members")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/", ListAsync)
            .WithName("AdminListOrganizationMembers")
            .WithSummary("List organization members");

        group.MapDelete("/{id}", RemoveAsync)
            .WithName("AdminRemoveMember")
            .WithSummary("Remove member from organization");

        group.MapPatch("/{id}/deactivate", DeactivateAsync)
            .WithName("AdminDeactivateMember")
            .WithSummary("Deactivate member in organization");

        group.MapPatch("/{id}/reactivate", ReactivateAsync)
            .WithName("AdminReactivateMember")
            .WithSummary("Reactivate member in organization");
    }

    private static async Task<IResult> ListAsync(
        string organizationId,
        ISender sender,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetAdminOrganizationMembersQuery(organizationId, page, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveAsync(
        string organizationId, string id,
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveMemberCommand(organizationId, id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateAsync(
        string organizationId, string id,
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AdminDeactivateMemberCommand(organizationId, id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateAsync(
        string organizationId, string id,
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new AdminReactivateMemberCommand(organizationId, id), cancellationToken);
        return result.ToHttpResult();
    }
}
