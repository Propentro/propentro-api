using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Contacts.Commands.ArchiveContact;
using Propentro.Application.Features.Contacts.Commands.CreateContact;
using Propentro.Application.Features.Contacts.Commands.DeleteContact;
using Propentro.Application.Features.Contacts.Commands.UnarchiveContact;
using Propentro.Application.Features.Contacts.Commands.UpdateContact;
using Propentro.Application.Features.Contacts.Queries.GetContactById;
using Propentro.Application.Features.Contacts.Queries.GetContacts;
using Propentro.Contracts.Contacts;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

internal static class ContactEndpoints
{
    public static void MapContactEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/contacts")
            .WithTags("Contacts")
            .RequireAuthorization();

        group.MapGet("/", GetContactsAsync)
            .WithDescription("List contacts with optional role, search, and archive filters")
            .WithMetadata(new RequirePermissionAttribute("contacts:read"))
            .Produces<PagedResult<ContactListItemResponse>>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        group.MapGet("/{id}", GetContactByIdAsync)
            .WithDescription("Get contact by ID")
            .WithMetadata(new RequirePermissionAttribute("contacts:read"))
            .Produces<ContactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/", CreateContactAsync)
            .WithDescription("Create a new contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:create"))
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPatch("/{id}", UpdateContactAsync)
            .WithDescription("Update an existing contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:update"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", DeleteContactAsync)
            .WithDescription("Soft delete a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id}/archive", ArchiveContactAsync)
            .WithDescription("Archive a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:update"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id}/unarchive", UnarchiveContactAsync)
            .WithDescription("Unarchive a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:update"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> GetContactsAsync(
        string? role,
        string? search,
        bool? isArchived,
        int page = 1,
        int pageSize = 25,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetContactsQuery(role, search, isArchived, page, pageSize);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetContactByIdAsync(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetContactByIdQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateContactAsync(
        CreateContactRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateContactCommand(
            request.Kind,
            request.DisplayName,
            request.Roles,
            request.FirstName,
            request.LastName,
            request.LegalName,
            request.TradingName,
            request.Email,
            request.Phone,
            request.SecondaryEmail,
            request.SecondaryPhone,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.Trade,
            request.TradeOther,
            request.TaxNumber,
            request.TaxCountry,
            request.Note,
            request.Address);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/v1/contacts/{id}");
    }

    private static async Task<IResult> UpdateContactAsync(
        string id,
        UpdateContactRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateContactCommand(
            id,
            request.DisplayName,
            request.FirstName,
            request.LastName,
            request.LegalName,
            request.TradingName,
            request.Email,
            request.Phone,
            request.SecondaryEmail,
            request.SecondaryPhone,
            request.EmergencyContactName,
            request.EmergencyContactPhone,
            request.Trade,
            request.TradeOther,
            request.TaxNumber,
            request.TaxCountry,
            request.Note,
            request.Address);

        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteContactAsync(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteContactCommand(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> ArchiveContactAsync(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ArchiveContactCommand(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UnarchiveContactAsync(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new UnarchiveContactCommand(id), ct);
        return result.ToHttpResult();
    }
}
