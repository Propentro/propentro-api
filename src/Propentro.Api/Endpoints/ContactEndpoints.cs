using MediatR;
using Propentro.Api.Extensions;
using Propentro.Application.Features.Activities.Commands.CreateActivity;
using Propentro.Application.Features.Activities.Commands.DeleteActivity;
using Propentro.Application.Features.Activities.Commands.UpdateActivity;
using Propentro.Application.Features.Activities.Queries.GetActivities;
using Propentro.Application.Features.Contacts.Commands.ConfirmContactDocumentUpload;
using Propentro.Application.Features.Contacts.Commands.InitiateContactDocumentUpload;
using Propentro.Application.Features.Contacts.Commands.InitiateContactDocumentVersionUpload;
using Propentro.Application.Features.Contacts.Commands.ArchiveContact;
using Propentro.Application.Features.Contacts.Commands.BulkArchiveContacts;
using Propentro.Application.Features.Contacts.Commands.BulkUnarchiveContacts;
using Propentro.Application.Features.Contacts.Commands.CreateContact;
using Propentro.Application.Features.Contacts.Commands.DeleteContact;
using Propentro.Application.Features.Contacts.Commands.RemoveContactDocument;
using Propentro.Application.Features.Contacts.Commands.SetContactTags;
using Propentro.Application.Features.Contacts.Commands.BulkTagContacts;
using Propentro.Application.Features.Contacts.Commands.BulkUntagContacts;
using Propentro.Application.Features.Contacts.Commands.UnarchiveContact;
using Propentro.Application.Features.Contacts.Commands.UpdateContact;
using Propentro.Application.Features.Contacts.Commands.UpdateContactDocument;
using Propentro.Application.Features.Contacts.Queries.GetContactById;
using Propentro.Application.Features.Contacts.Queries.GetContactDocuments;
using Propentro.Application.Features.Contacts.Queries.GetContacts;
using Propentro.Contracts.Activities;
using Propentro.Contracts.Contacts;
using Propentro.Contracts.Documents;
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
            .WithName("GetContacts")
            .WithSummary("List contacts with search and filters")
            .WithMetadata(new RequirePermissionAttribute("contacts:read"))
            .Produces<Application.Common.Models.PagedResult<ContactListItemResponse>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/", CreateContactAsync)
            .WithName("CreateContact")
            .WithSummary("Create a new contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id}", GetContactByIdAsync)
            .WithName("GetContactById")
            .WithSummary("Get contact by ID")
            .WithMetadata(new RequirePermissionAttribute("contacts:read"))
            .Produces<ContactResponse>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/{id}", UpdateContactAsync)
            .WithName("UpdateContact")
            .WithSummary("Update a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}", DeleteContactAsync)
            .WithName("DeleteContact")
            .WithSummary("Delete a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/archive", ArchiveContactAsync)
            .WithName("ArchiveContact")
            .WithSummary("Archive a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id}/unarchive", UnarchiveContactAsync)
            .WithName("UnarchiveContact")
            .WithSummary("Unarchive a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/bulk-archive", BulkArchiveAsync)
            .WithName("BulkArchiveContacts")
            .WithSummary("Archive multiple contacts")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/bulk-unarchive", BulkUnarchiveAsync)
            .WithName("BulkUnarchiveContacts")
            .WithSummary("Unarchive multiple contacts")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        // Activities
        group.MapGet("/{id}/activities", GetContactActivitiesAsync)
            .WithName("GetContactActivities")
            .WithSummary("List activities for a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:read"))
            .Produces<Application.Common.Models.PagedResult<ActivityResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/activities", CreateContactActivityAsync)
            .WithName("CreateContactActivity")
            .WithSummary("Add an activity to a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces<object>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPatch("/{id}/activities/{activityId}", UpdateContactActivityAsync)
            .WithName("UpdateContactActivity")
            .WithSummary("Update a contact activity")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}/activities/{activityId}", DeleteContactActivityAsync)
            .WithName("DeleteContactActivity")
            .WithSummary("Delete a contact activity")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Documents
        group.MapGet("/{id}/documents", GetContactDocumentsAsync)
            .WithName("GetContactDocuments")
            .WithSummary("List documents for a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:read"))
            .Produces<IReadOnlyList<ContactDocumentResponse>>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/documents/initiate-upload", InitiateContactDocumentUploadAsync)
            .WithName("InitiateContactDocumentUpload")
            .WithSummary("Initiate a presigned document upload for a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces<ContactDocumentUploadResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/{id}/documents/{documentId}/confirm-upload", ConfirmContactDocumentUploadAsync)
            .WithName("ConfirmContactDocumentUpload")
            .WithSummary("Confirm a document upload after client-side upload to storage")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPost("/{id}/documents/{documentId}/initiate-version-upload", InitiateContactDocumentVersionUploadAsync)
            .WithName("InitiateContactDocumentVersionUpload")
            .WithSummary("Initiate a new version upload for an existing document")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces<ContactDocumentUploadResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPatch("/{id}/documents/{documentId}", UpdateContactDocumentAsync)
            .WithName("UpdateContactDocument")
            .WithSummary("Update a contact document")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapDelete("/{id}/documents/{documentId}", RemoveContactDocumentAsync)
            .WithName("RemoveContactDocument")
            .WithSummary("Remove a document from a contact")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound);

        // Tags
        group.MapPut("/{id}/tags", SetContactTagsAsync)
            .WithName("SetContactTags")
            .WithSummary("Set tags on a contact (declarative)")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/bulk-tag", BulkTagAsync)
            .WithName("BulkTagContacts")
            .WithSummary("Add tags to multiple contacts")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPost("/bulk-untag", BulkUntagAsync)
            .WithName("BulkUntagContacts")
            .WithSummary("Remove tags from multiple contacts")
            .WithMetadata(new RequirePermissionAttribute("contacts:write"))
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> GetContactsAsync(
        int page,
        int pageSize,
        string? role,
        string? search,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetContactsQuery(page, pageSize, role, search);
        var result = await sender.Send(query, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateContactAsync(
        CreateContactRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateContactCommand(
            request.Kind,
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
            request.Address is not null
                ? new CreateContactAddressDto(
                    request.Address.Line1,
                    request.Address.Line2,
                    request.Address.City,
                    request.Address.Region,
                    request.Address.PostalCode,
                    request.Address.CountryCode)
                : null);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(id => $"/v1/contacts/{id}");
    }

    private static async Task<IResult> GetContactByIdAsync(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetContactByIdQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> UpdateContactAsync(
        string id,
        UpdateContactRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateContactCommand(
            id,
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
            request.Address is not null
                ? new UpdateContactAddressDto(
                    request.Address.Line1,
                    request.Address.Line2,
                    request.Address.City,
                    request.Address.Region,
                    request.Address.PostalCode,
                    request.Address.CountryCode)
                : null);

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

    private static async Task<IResult> BulkArchiveAsync(
        BulkContactIdsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new BulkArchiveContactsCommand(request.ContactIds), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> BulkUnarchiveAsync(
        BulkContactIdsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new BulkUnarchiveContactsCommand(request.ContactIds), ct);
        return result.ToHttpResult();
    }

    // Activity handlers

    private static async Task<IResult> GetContactActivitiesAsync(
        string id,
        int page,
        int pageSize,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetActivitiesQuery("contact", id, page, pageSize), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> CreateContactActivityAsync(
        string id,
        CreateActivityRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new CreateActivityCommand(
            "contact",
            id,
            request.ActivityType,
            request.Title,
            request.Description);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(actId => $"/v1/contacts/{id}/activities/{actId}");
    }

    private static async Task<IResult> UpdateContactActivityAsync(
        string id,
        string activityId,
        UpdateActivityRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateActivityCommand(activityId, request.Description, request.IsPinned);
        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> DeleteContactActivityAsync(
        string id,
        string activityId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new DeleteActivityCommand(activityId), ct);
        return result.ToHttpResult();
    }

    // Document handlers

    private static async Task<IResult> GetContactDocumentsAsync(
        string id,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new GetContactDocumentsQuery(id), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> InitiateContactDocumentUploadAsync(
        string id,
        InitiateContactDocumentUploadRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new InitiateContactDocumentUploadCommand(
            id,
            request.Name,
            request.DocumentType,
            request.Description,
            request.IsPrimary,
            request.FileName,
            request.ContentType,
            request.FileSize);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(r => $"/v1/contacts/{id}/documents/{r.DocumentId}");
    }

    private static async Task<IResult> ConfirmContactDocumentUploadAsync(
        string id,
        string documentId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new ConfirmContactDocumentUploadCommand(id, documentId), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> InitiateContactDocumentVersionUploadAsync(
        string id,
        string documentId,
        InitiateContactDocumentVersionUploadRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new InitiateContactDocumentVersionUploadCommand(
            id,
            documentId,
            request.FileName,
            request.ContentType,
            request.FileSize);

        var result = await sender.Send(command, ct);
        return result.ToCreatedResult(r => $"/v1/contacts/{id}/documents/{r.DocumentId}");
    }

    private static async Task<IResult> UpdateContactDocumentAsync(
        string id,
        string documentId,
        UpdateContactDocumentRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new UpdateContactDocumentCommand(
            id,
            documentId,
            request.Name,
            request.Description,
            request.IsPrimary);

        var result = await sender.Send(command, ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> RemoveContactDocumentAsync(
        string id,
        string documentId,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new RemoveContactDocumentCommand(id, documentId), ct);
        return result.ToHttpResult();
    }

    // Tag handlers

    private static async Task<IResult> SetContactTagsAsync(
        string id,
        SetContactTagsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new SetContactTagsCommand(id, request.TagIds), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> BulkTagAsync(
        BulkContactTagsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new BulkTagContactsCommand(request.ContactIds, request.TagIds), ct);
        return result.ToHttpResult();
    }

    private static async Task<IResult> BulkUntagAsync(
        BulkContactTagsRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(new BulkUntagContactsCommand(request.ContactIds, request.TagIds), ct);
        return result.ToHttpResult();
    }
}
