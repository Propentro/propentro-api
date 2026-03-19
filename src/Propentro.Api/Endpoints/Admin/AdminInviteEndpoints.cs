using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Features.Admin.Invites.Commands.InviteUser;
using Propentro.Application.Features.Admin.Invites.Queries.GetInvites;
using Propentro.Application.Features.Admin.Invites.Queries.GetOrganizationInvites;
using Propentro.Application.Features.Organizations.Invites.Commands.CancelInvite;
using Propentro.Contracts.Admin.Invites;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Admin;

internal static class AdminInviteEndpoints
{
    public static void MapAdminInviteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/admin")
            .WithTags("Admin Invites")
            .RequireAuthorization()
            .WithMetadata(new RequireSystemAdministratorAttribute());

        group.MapGet("/invites", ListAsync)
            .WithName("AdminListInvites")
            .WithSummary("List all invites");

        group.MapPost("/organizations/{organizationId}/invites", InviteAsync)
            .WithName("AdminInviteUser")
            .WithSummary("Invite user to organization");

        group.MapGet("/organizations/{organizationId}/invites", ListOrgInvitesAsync)
            .WithName("AdminListOrganizationInvites")
            .WithSummary("List organization invites");

        group.MapPatch("/invites/{id}/cancel", CancelAsync)
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
        string organizationId,
        InviteUserRequest request,
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var command = new InviteUserCommand(
            organizationId, request.Email, request.RoleId, userContext.UserId);
        var result = await sender.Send(command, cancellationToken);
        return result.Match(
            inviteResult => inviteResult.IsDirectAdd
                ? Results.Ok(inviteResult)
                : Results.Created($"/invites/{inviteResult.InviteId}", inviteResult),
            error => error.ToHttpResult());
    }

    private static async Task<IResult> ListOrgInvitesAsync(
        string organizationId,
        ISender sender,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetOrganizationInvitesQuery(organizationId, page, pageSize), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CancelAsync(
        string id,
        ISender sender,
        IUserContext userContext,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(
            new CancelInviteCommand(id, userContext.UserId), cancellationToken);
        return result.ToHttpResult();
    }
}
