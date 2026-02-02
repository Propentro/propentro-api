using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Units.Commands.CreateUnit;
using Propentro.Application.Units.Commands.UpdateUnit;
using Propentro.Application.Units.Commands.DeleteUnit;
using Propentro.Application.Units.Queries.GetUnits;
using Propentro.Application.Units.Queries.GetUnitById;
using Propentro.Contracts.Units;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

public static class UnitEndpoints
{
    public static void MapUnitEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/units")
            .WithTags("Units")
            .RequireAuthorization();

        group.MapGet("/", GetUnits)
            .WithName("GetUnits")
            .WithSummary("Get all units")
            .WithMetadata(new RequirePermissionAttribute("units:read"))
            .Produces<PagedResult<UnitDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{publicId}", GetUnitById)
            .WithName("GetUnitById")
            .WithSummary("Get a unit by public ID")
            .WithMetadata(new RequirePermissionAttribute("units:read"))
            .Produces<UnitDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateUnit)
            .WithName("CreateUnit")
            .WithSummary("Create a new unit")
            .WithMetadata(new RequirePermissionAttribute("units:create"))
            .Produces<string>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{publicId}", UpdateUnit)
            .WithName("UpdateUnit")
            .WithSummary("Update a unit")
            .WithMetadata(new RequirePermissionAttribute("units:update"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/{publicId}", DeleteUnit)
            .WithName("DeleteUnit")
            .WithSummary("Delete a unit")
            .WithMetadata(new RequirePermissionAttribute("units:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetUnits(
        ISender sender,
        [AsParameters] GetUnitsQuery query,
        CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetUnitById(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetUnitByIdQuery(publicId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateUnit(
        CreateUnitRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateUnitCommand(
            request.PropertyId,
            request.Name,
            request.UnitType,
            request.UsageType,
            request.RentalMode,
            request.Floor,
            request.UnitNumber,
            request.Bedrooms,
            request.Bathrooms,
            request.AreaSqm,
            request.DefaultRentMinor,
            request.DefaultDepositMinor,
            request.DefaultCurrency,
            request.Note);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/api/v1/units/{id}");
    }

    private static async Task<IResult> UpdateUnit(
        string publicId,
        UpdateUnitRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateUnitCommand(
            publicId,
            request.Name,
            request.UnitType,
            request.UsageType,
            request.RentalMode,
            request.Status,
            request.Floor,
            request.UnitNumber,
            request.Bedrooms,
            request.Bathrooms,
            request.AreaSqm,
            request.DefaultRentMinor,
            request.DefaultDepositMinor,
            request.DefaultCurrency,
            request.Note);

        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteUnit(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteUnitCommand(publicId), ct);
        return result.ToHttpResult();
    }
}
