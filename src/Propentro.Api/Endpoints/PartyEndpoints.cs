using MediatR;
using Propentro.Application.Parties.Commands.CreateParty;
using Propentro.Application.Parties.Commands.UpdateParty;
using Propentro.Application.Parties.Commands.DeleteParty;
using Propentro.Application.Parties.Queries.GetParties;
using Propentro.Application.Parties.Queries.GetPartyById;
using Propentro.Contracts.Parties;

namespace Propentro.Api.Endpoints;

public static class PartyEndpoints
{
    public static void MapPartyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/parties")
            .WithTags("Parties")
            .RequireAuthorization();

        group.MapGet("/", GetParties)
            .WithName("GetParties")
            .WithSummary("Get all parties");

        group.MapGet("/{publicId}", GetPartyById)
            .WithName("GetPartyById")
            .WithSummary("Get a party by public ID");

        group.MapPost("/", CreateParty)
            .WithName("CreateParty")
            .WithSummary("Create a new party");

        group.MapPut("/{publicId}", UpdateParty)
            .WithName("UpdateParty")
            .WithSummary("Update a party");

        group.MapDelete("/{publicId}", DeleteParty)
            .WithName("DeleteParty")
            .WithSummary("Delete a party");
    }

    private static async Task<IResult> GetParties(
        ISender sender,
        [AsParameters] GetPartiesQuery query,
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

    private static async Task<IResult> GetPartyById(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetPartyByIdQuery(publicId), ct);

        return result.Match(
            success => Results.Ok(success),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
    }

    private static async Task<IResult> CreateParty(
        CreatePartyRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreatePartyCommand(
            request.Kind,
            request.DisplayName,
            request.FirstName,
            request.LastName,
            request.LegalName,
            request.TradingName,
            request.Email,
            request.Phone,
            request.TaxNumber,
            request.TaxCountry,
            request.Note);

        var result = await sender.Send(command, ct);

        return result.Match(
            success => Results.Created($"/api/v1/parties/{success}", new { publicId = success }),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
    }

    private static async Task<IResult> UpdateParty(
        string publicId,
        UpdatePartyRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdatePartyCommand(
            publicId,
            request.DisplayName,
            request.FirstName,
            request.LastName,
            request.LegalName,
            request.TradingName,
            request.Email,
            request.Phone,
            request.TaxNumber,
            request.TaxCountry,
            request.Note);

        var result = await sender.Send(command, ct);

        return result.Match(
            () => Results.NoContent(),
            error => Results.Problem(
                statusCode: GetStatusCode(error.Type),
                title: error.Code,
                detail: error.Message));
    }

    private static async Task<IResult> DeleteParty(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeletePartyCommand(publicId), ct);

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
