using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Parties.Commands.CreateParty;
using Propentro.Application.Parties.Commands.UpdateParty;
using Propentro.Application.Parties.Commands.DeleteParty;
using Propentro.Application.Parties.Queries.GetParties;
using Propentro.Application.Parties.Queries.GetPartyById;
using Propentro.Contracts.Parties;
using Propentro.Infrastructure.Authorization;

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
            .WithSummary("Get all parties")
            .WithMetadata(new RequirePermissionAttribute("parties:read"))
            .Produces<PagedResult<PartyDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{publicId}", GetPartyById)
            .WithName("GetPartyById")
            .WithSummary("Get a party by public ID")
            .WithMetadata(new RequirePermissionAttribute("parties:read"))
            .Produces<PartyDetailDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/", CreateParty)
            .WithName("CreateParty")
            .WithSummary("Create a new party")
            .WithMetadata(new RequirePermissionAttribute("parties:create"))
            .Produces<string>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPut("/{publicId}", UpdateParty)
            .WithName("UpdateParty")
            .WithSummary("Update a party")
            .WithMetadata(new RequirePermissionAttribute("parties:update"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/{publicId}", DeleteParty)
            .WithName("DeleteParty")
            .WithSummary("Delete a party")
            .WithMetadata(new RequirePermissionAttribute("parties:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    private static async Task<IResult> GetParties(
        ISender sender,
        [AsParameters] GetPartiesQuery query,
        CancellationToken ct)
    {
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetPartyById(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetPartyByIdQuery(publicId), ct);
        return result.ToHttpResult();
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
        return result.ToCreatedResult(id => $"/api/v1/parties/{id}");
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
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteParty(
        string publicId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeletePartyCommand(publicId), ct);
        return result.ToHttpResult();
    }
}
