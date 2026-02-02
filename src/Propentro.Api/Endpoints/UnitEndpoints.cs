using MediatR;
using Propentro.Application.Units.Commands.CreateUnit;
using Propentro.Application.Units.Commands.UpdateUnit;
using Propentro.Application.Units.Commands.DeleteUnit;
using Propentro.Application.Units.Queries.GetUnits;
using Propentro.Application.Units.Queries.GetUnitById;
using Propentro.Contracts.Units;

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
            .WithSummary("Get all units");

        group.MapGet("/{publicId}", GetUnitById)
            .WithName("GetUnitById")
            .WithSummary("Get a unit by public ID");

        group.MapPost("/", CreateUnit)
            .WithName("CreateUnit")
            .WithSummary("Create a new unit");

        group.MapPut("/{publicId}", UpdateUnit)
            .WithName("UpdateUnit")
            .WithSummary("Update a unit");

        group.MapDelete("/{publicId}", DeleteUnit)
            .WithName("DeleteUnit")
            .WithSummary("Delete a unit");
    }

    private static async Task<IResult> GetUnits(
        ISender sender,
        [AsParameters] GetUnitsQuery query,
        CancellationToken ct)
    {
        var result = await sender.Send(query, ct);

        return result.Match(
            success => Results.Ok(success),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
    }

    private static async Task<IResult> GetUnitById(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetUnitByIdQuery(publicId), ct);

        return result.Match(
            success => Results.Ok(success),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
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

        return result.Match(
            success => Results.Created($"/api/v1/units/{success}", new { publicId = success }),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
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

        return result.Match(
            () => Results.NoContent(),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
    }

    private static async Task<IResult> DeleteUnit(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteUnitCommand(publicId), ct);

        return result.Match(
            () => Results.NoContent(),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
    }

    private static int GetStatusCode(Application.Common.Models.ErrorType errorType) => errorType switch
    {
        Application.Common.Models.ErrorType.NotFound => StatusCodes.Status404NotFound,
        Application.Common.Models.ErrorType.Validation => StatusCodes.Status400BadRequest,
        Application.Common.Models.ErrorType.Conflict => StatusCodes.Status409Conflict,
        Application.Common.Models.ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        Application.Common.Models.ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status500InternalServerError
    };
}
