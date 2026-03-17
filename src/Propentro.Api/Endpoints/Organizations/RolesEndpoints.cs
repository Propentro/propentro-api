using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Roles.Commands.CreateRole;
using Propentro.Application.Features.Organizations.Roles.Commands.DeleteRole;
using Propentro.Application.Features.Organizations.Roles.Commands.UpdateRole;
using Propentro.Application.Features.Organizations.Roles.Permissions.Queries.GetPermissions;
using Propentro.Application.Features.Organizations.Roles.Queries.GetOrganizationRoles;
using Propentro.Application.Features.Organizations.Roles.Queries.GetRole;
using Propentro.Contracts.Organizations.Roles;
using Propentro.Contracts.Organizations.Roles.Permissions;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints.Organizations;

internal static class RolesEndpoints
{
    public static void MapOrganizationRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/organizations/roles")
            .WithTags("Organization Roles")
            .RequireAuthorization();

        group.MapGet("/", GetRolesAsync)
            .WithName("GetOrganizationRoles")
            .WithSummary("Get all roles for the organization")
            .WithMetadata(new RequirePermissionAttribute("org:roles:read"))
            .Produces<PagedResult<RoleResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        group.MapGet("/{id}", GetRoleAsync)
            .WithName("GetOrganizationRole")
            .WithSummary("Get role detail")
            .WithMetadata(new RequirePermissionAttribute("org:roles:read"));

        group.MapPost("/", CreateRoleAsync)
            .WithName("CreateRole")
            .WithSummary("Create a custom role")
            .WithMetadata(new RequirePermissionAttribute("org:roles:manage"))
            .Produces<string>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPatch("/{roleId}", UpdateRoleAsync)
            .WithName("UpdateRole")
            .WithSummary("Update a custom role")
            .WithMetadata(new RequirePermissionAttribute("org:roles:manage"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{roleId}", DeleteRoleAsync)
            .WithName("DeleteRole")
            .WithSummary("Delete a custom role")
            .WithMetadata(new RequirePermissionAttribute("org:roles:manage"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        var permissionsGroup = app.MapGroup("/v1/organizations/permissions")
            .WithTags("Organization Roles")
            .RequireAuthorization();

        permissionsGroup.MapGet("/", GetPermissionsAsync)
            .WithName("GetOrganizationPermissions")
            .WithSummary("Get all available permissions grouped by category")
            .WithMetadata(new RequirePermissionAttribute("org:roles:read"))
            .Produces<IReadOnlyList<PermissionGroupResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetRolesAsync(
        ISender sender,
        int page = 1,
        int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetOrganizationRolesQuery(page, pageSize), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetRoleAsync(
        string id, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetRoleQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateRoleAsync(CreateRoleRequest request, ISender sender, CancellationToken ct)
    {
        var command = new CreateRoleCommand(request.Name, request.Description, request.PermissionCodes);
        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/v1/organizations/roles/{id}");
    }

    private static async Task<IResult> UpdateRoleAsync(
        string roleId,
        UpdateRoleRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateRoleCommand(roleId, request.Name, request.Description, request.PermissionCodes);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteRoleAsync(string roleId, ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new DeleteRoleCommand(roleId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPermissionsAsync(ISender sender, CancellationToken ct)
    {
        var result = await sender.Send(new GetPermissionsQuery(), ct);
        return result.ToHttpResult();
    }

}
