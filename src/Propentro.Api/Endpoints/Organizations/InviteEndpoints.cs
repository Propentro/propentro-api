using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Features.Organizations.Invites.Commands.CancelInvite;
using Propentro.Application.Features.Organizations.Invites.Commands.InviteToCurrentOrg;
using Propentro.Application.Features.Organizations.Invites.Queries.GetCurrentOrgInvites;
using Propentro.Contracts.Admin.Invites;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Organizations;

internal static class InviteEndpoints
{
    public static void MapOrganizationInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/organizations/invites")
            .WithTags("Organization Invites")
            .RequireAuthorization();

        group.MapPost("/", InviteAsync)
            .WithName("InviteToOrganization")
            .WithSummary("Invite member to current organization")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"));

        group.MapGet("/", ListAsync)
            .WithName("ListOrganizationInvites")
            .WithSummary("List pending invites in current organization")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"));

        group.MapPatch("/{id}/cancel", CancelAsync)
            .WithName("CancelOrganizationInvite")
            .WithSummary("Cancel a pending invite")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"));
    }

    private static async Task<IResult> InviteAsync(
        InviteUserRequest request,
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new InviteToCurrentOrgCommand(
            request.Email, request.RoleId,
            userContext.OrganizationId, userContext.UserId);
        var result = await sender.Send(command, cancellationToken);
        return result.Match(
            inviteResult => inviteResult.IsDirectAdd
                ? Results.Ok(inviteResult)
                : Results.Created($"/invites/{inviteResult.InviteId}", inviteResult),
            error => error.ToHttpResult());
    }

    private static async Task<IResult> ListAsync(
        ISender sender,
        IUserContext userContext,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetCurrentOrgInvitesQuery(userContext.OrganizationId, page, pageSize),
            cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelAsync(
        string id,
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelInviteCommand(id, userContext.UserId, userContext.OrganizationId),
            cancellationToken);
        return result.ToHttpResult();
    }
}
