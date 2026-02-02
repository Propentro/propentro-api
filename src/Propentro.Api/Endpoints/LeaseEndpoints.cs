using MediatR;
using Propentro.Application.Leases.Commands.CreateLease;
using Propentro.Application.Leases.Commands.UpdateLease;
using Propentro.Application.Leases.Commands.DeleteLease;
using Propentro.Application.Leases.Queries.GetLeases;
using Propentro.Application.Leases.Queries.GetLeaseById;
using Propentro.Contracts.Leases;

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
            .WithSummary("Get all leases");

        group.MapGet("/{publicId}", GetLeaseById)
            .WithName("GetLeaseById")
            .WithSummary("Get a lease by public ID");

        group.MapPost("/", CreateLease)
            .WithName("CreateLease")
            .WithSummary("Create a new lease");

        group.MapPut("/{publicId}", UpdateLease)
            .WithName("UpdateLease")
            .WithSummary("Update a lease");

        group.MapDelete("/{publicId}", DeleteLease)
            .WithName("DeleteLease")
            .WithSummary("Delete a lease");
    }

    private static async Task<IResult> GetLeases(
        ISender sender,
        [AsParameters] GetLeasesQuery query,
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

    private static async Task<IResult> GetLeaseById(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetLeaseByIdQuery(publicId), ct);

        return result.Match(
            success => Results.Ok(success),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
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

        return result.Match(
            success => Results.Created($"/api/v1/leases/{success}", new { publicId = success }),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
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

        return result.Match(
            () => Results.NoContent(),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
    }

    private static async Task<IResult> DeleteLease(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteLeaseCommand(publicId), ct);

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
