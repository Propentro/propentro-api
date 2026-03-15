using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Features.Admin.Invites.Commands.CancelInvite;
using Propentro.Application.Features.Admin.Invites.Commands.InviteUser;
using Propentro.Application.Features.Admin.Invites.Queries.GetInvites;
using Propentro.Application.Features.Admin.Invites.Queries.GetOrganizationInvites;
using Propentro.Contracts.Admin.Invites;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminInviteEndpoints
{
    public static void MapAdminInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin")
            .WithTags("Admin — Invites")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/invites", ListAsync)
            .WithName("AdminListInvites")
            .WithSummary("List all invites");

        group.MapPost("/organisations/{orgPublicId}/invites", InviteAsync)
            .WithName("AdminInviteUser")
            .WithSummary("Invite user to organisation");

        group.MapGet("/organisations/{orgPublicId}/invites", ListOrgInvitesAsync)
            .WithName("AdminListOrganisationInvites")
            .WithSummary("List organisation invites");

        group.MapDelete("/invites/{invitePublicId}", CancelAsync)
            .WithName("AdminCancelInvite")
            .WithSummary("Cancel invite");
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        string? status,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetInvitesQuery(status, page, pageSize), cancellationToken);
        return result.ToHttpResult();
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

    private static async Task<IResult> ListOrgInvitesAsync(
        string orgPublicId,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new GetOrganizationInvitesQuery(orgPublicId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelAsync(
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
