using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Features.Admin.Invites.Commands.CancelInvite;
using Propentro.Application.Features.Admin.Invites.Commands.InviteUser;
using Propentro.Application.Features.Admin.Invites.Queries.GetOrganizationInvites;
using Propentro.Contracts.Admin.Invites;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Organizations;

internal static class InviteEndpoints
{
    public static void MapOrganizationInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/organisations/{orgPublicId}/invites")
            .WithTags("Organisation Invites")
            .RequireAuthorization();

        group.MapPost("/", InviteAsync)
            .WithName("InviteOrgMember")
            .WithSummary("Invite member to organisation")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"));

        group.MapGet("/", ListAsync)
            .WithName("ListOrgInvites")
            .WithSummary("List pending invites in organisation")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"));

        group.MapDelete("/{invitePublicId}", CancelAsync)
            .WithName("CancelOrgInvite")
            .WithSummary("Cancel invite in organisation")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"));
    }

    private static async Task<IResult> InviteAsync(
        string orgPublicId,
        InviteUserRequest request,
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new InviteUserCommand(
            orgPublicId, request.Email, request.RoleId, userContext.UserId);
        var result = await sender.Send(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ListAsync(
        string orgPublicId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOrganizationInvitesQuery(orgPublicId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelAsync(
        string orgPublicId,
        string invitePublicId,
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelInviteCommand(invitePublicId, userContext.UserId), cancellationToken);
        return result.ToHttpResult();
    }
}
