using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Admin.Users.Commands.RemoveMember;
using Propentro.Application.Features.Admin.Users.Queries.GetOrganizationMembers;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminMemberEndpoints
{
    public static void MapAdminMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin/organizations/{orgPublicId}/members")
            .WithTags("Admin Members")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/", ListAsync)
            .WithName("AdminListOrganizationMembers")
            .WithSummary("List organization members");

        group.MapDelete("/{userPublicId}", RemoveAsync)
            .WithName("AdminRemoveMember")
            .WithSummary("Remove member from organization");
    }

    private static async Task<IResult> ListAsync(
        string orgPublicId, ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetAdminOrganizationMembersQuery(orgPublicId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveAsync(
        string orgPublicId, string userPublicId,
        ISender sender, CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new RemoveMemberCommand(orgPublicId, userPublicId), cancellationToken);
        return result.ToHttpResult();
    }
}
