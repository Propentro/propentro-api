using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members.Commands.DeactivateMember;
using Propentro.Application.Features.Organizations.Members.Queries.GetMember;
using Propentro.Application.Features.Organizations.Members.Commands.ReactivateMember;
using Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;
using Propentro.Application.Features.Organizations.Members.Queries.GetOrganizationMembers;
using Propentro.Contracts.Organizations.Members;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Organizations;

internal static class MembersEndpoints
{
    public static void MapOrganizationMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/organizations/members")
            .WithTags("Organization Members")
            .RequireAuthorization();

        group.MapGet("/", GetMembersAsync)
            .WithName("GetOrganizationMembers")
            .WithSummary("Get organization members")
            .WithMetadata(new RequirePermissionAttribute("org:members:read"))
            .Produces<PagedResult<MemberListItemResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id}", GetMemberAsync)
            .WithName("GetOrganizationMember")
            .WithSummary("Get member detail")
            .WithMetadata(new RequirePermissionAttribute("org:members:read"));

        group.MapPatch("/{userId}", UpdateMemberRolesAsync)
            .WithName("UpdateMemberRoles")
            .WithSummary("Update member roles")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{userId}/deactivate", DeactivateMemberAsync)
            .WithName("DeactivateMember")
            .WithSummary("Deactivate a member from the organization")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{userId}/reactivate", ReactivateMemberAsync)
            .WithName("ReactivateMember")
            .WithSummary("Reactivate a deactivated member")
            .WithMetadata(new RequirePermissionAttribute("org:members:manage"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

    }

    private static async Task<IResult> GetMembersAsync(
        [AsParameters] GetMembersQueryParams queryParams,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetOrganizationMembersQuery(
            queryParams.Page,
            queryParams.PageSize,
            queryParams.Search,
            queryParams.Status,
            queryParams.RoleId);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetMemberAsync(
        string id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetMemberQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateMemberRolesAsync(
        string userId,
        UpdateMemberRolesRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateMemberRolesCommand(userId, request.RoleIds);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeactivateMemberAsync(
        string userId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeactivateMemberCommand(userId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ReactivateMemberAsync(
        string userId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ReactivateMemberCommand(userId), ct);
        return result.ToHttpResult();
    }

    private sealed record GetMembersQueryParams(
        int Page = 1,
        int PageSize = 25,
        string? Search = null,
        string? Status = null,
        string? RoleId = null);
}
