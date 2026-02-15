using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Activities.Commands.CreateActivity;
using Propentro.Application.Features.Activities.Commands.DeleteActivity;
using Propentro.Application.Features.Activities.Commands.UpdateActivity;
using Propentro.Application.Features.Activities.Queries.GetActivities;
using Propentro.Contracts.Activities;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

internal static class ActivityEndpoints
{
    public static void MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var contactActivities = app.MapGroup("/v1/contacts/{contactId}/activities")
            .WithTags("Activities")
            .RequireAuthorization();

        contactActivities.MapGet("/", GetActivitiesAsync)
            .WithDescription("List activities for a contact")
            .WithMetadata(new RequirePermissionAttribute("activities:read"))
            .Produces<PagedResult<ActivityResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        contactActivities.MapPost("/", CreateActivityAsync)
            .WithDescription("Create a new activity for a contact")
            .WithMetadata(new RequirePermissionAttribute("activities:create"))
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        var activities = app.MapGroup("/v1/activities")
            .WithTags("Activities")
            .RequireAuthorization();

        activities.MapPatch("/{id}", UpdateActivityAsync)
            .WithDescription("Update an activity")
            .WithMetadata(new RequirePermissionAttribute("activities:update"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        activities.MapDelete("/{id}", DeleteActivityAsync)
            .WithDescription("Soft delete an activity")
            .WithMetadata(new RequirePermissionAttribute("activities:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetActivitiesAsync(
        string contactId,
        int page = 1,
        int pageSize = 25,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetActivitiesQuery(contactId, page, pageSize);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateActivityAsync(
        string contactId,
        CreateActivityRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateActivityCommand(
            contactId,
            request.ActivityType,
            request.Title,
            request.Description);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/v1/activities/{id}");
    }

    private static async Task<IResult> UpdateActivityAsync(
        string id,
        UpdateActivityRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateActivityCommand(id, request.Description, request.IsPinned);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteActivityAsync(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteActivityCommand(id), ct);
        return result.ToHttpResult();
    }
}
