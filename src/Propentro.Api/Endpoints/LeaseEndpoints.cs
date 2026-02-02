using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Leases.Commands.CreateLease;
using Propentro.Application.Leases.Commands.UpdateLease;
using Propentro.Application.Leases.Commands.DeleteLease;
using Propentro.Application.Leases.Queries.GetLeases;
using Propentro.Application.Leases.Queries.GetLeaseById;
using Propentro.Contracts.Leases;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

public static class LeaseEndpoints
{
    public static void MapLeaseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/leases")
            .WithTags("Leases")
            .RequireAuthorization();

        group.MapGet("/", GetLeases)
            .WithName("GetLeases")
            .WithSummary("Get all leases")
            .WithMetadata(new RequirePermissionAttribute("leases:read"))
            .Produces<PagedResult<LeaseDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{publicId}", GetLeaseById)
            .WithName("GetLeaseById")
            .WithSummary("Get a lease by public ID")
            .WithMetadata(new RequirePermissionAttribute("leases:read"))
            .Produces<LeaseDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateLease)
            .WithName("CreateLease")
            .WithSummary("Create a new lease")
            .WithMetadata(new RequirePermissionAttribute("leases:create"))
            .Produces<string>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{publicId}", UpdateLease)
            .WithName("UpdateLease")
            .WithSummary("Update a lease")
            .WithMetadata(new RequirePermissionAttribute("leases:update"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/{publicId}", DeleteLease)
            .WithName("DeleteLease")
            .WithSummary("Delete a lease")
            .WithMetadata(new RequirePermissionAttribute("leases:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetLeases(
        ISender sender,
        [AsParameters] GetLeasesQuery query,
        CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetLeaseById(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetLeaseByIdQuery(publicId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateLease(
        CreateLeaseRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateLeaseCommand(
            request.LeaseType,
            request.StartDate,
            request.EndDate,
            request.RentMinor,
            request.Currency,
            request.RentFrequency,
            request.RentDueDay,
            request.DepositMinor,
            request.NoticePeriodDays,
            request.ReferenceCode,
            request.Note);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/api/v1/leases/{id}");
    }

    private static async Task<IResult> UpdateLease(
        string publicId,
        UpdateLeaseRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateLeaseCommand(
            publicId,
            request.Status,
            request.EndDate,
            request.MoveOutDate,
            request.RentMinor,
            request.RentFrequency,
            request.RentDueDay,
            request.DepositStatus,
            request.NoticePeriodDays,
            request.Note);

        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteLease(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteLeaseCommand(publicId), ct);
        return result.ToHttpResult();
    }
}
