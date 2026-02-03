using Propentro.Contracts.Documents;
using Propentro.Infrastructure.Authorization;

namespace Propentro.Api.Endpoints;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/documents")
            .WithTags("Documents")
            .RequireAuthorization();

        group.MapGet("/", GetDocuments)
            .WithName("GetDocuments")
            .WithSummary("Get all documents")
            .WithMetadata(new RequirePermissionAttribute("documents:read"))
            .Produces<IEnumerable<DocumentDto>>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{publicId}", GetDocumentById)
            .WithName("GetDocumentById")
            .WithSummary("Get a document by public ID")
            .WithMetadata(new RequirePermissionAttribute("documents:read"))
            .Produces<DocumentDto>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapPost("/upload", RequestUploadUrl)
            .WithName("RequestDocumentUpload")
            .WithSummary("Request a presigned URL for document upload")
            .WithMetadata(new RequirePermissionAttribute("documents:create"))
            .Produces<UploadDocumentResponse>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapGet("/{publicId}/download", GetDownloadUrl)
            .WithName("GetDocumentDownloadUrl")
            .WithSummary("Get a presigned URL for document download")
            .WithMetadata(new RequirePermissionAttribute("documents:read"))
            .Produces<string>()
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);

        group.MapDelete("/{publicId}", DeleteDocument)
            .WithName("DeleteDocument")
            .WithSummary("Delete a document")
            .WithMetadata(new RequirePermissionAttribute("documents:delete"))
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden);
    }

    // TODO: Implement GetDocumentsQuery in Application/Documents/Queries/GetDocuments/
    private static Task<IResult> GetDocuments(CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement GetDocumentByIdQuery in Application/Documents/Queries/GetDocumentById/
    private static Task<IResult> GetDocumentById(string publicId, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement RequestUploadCommand in Application/Documents/Commands/RequestUpload/
    private static Task<IResult> RequestUploadUrl(UploadDocumentRequest request, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement GetDownloadUrlQuery in Application/Documents/Queries/GetDownloadUrl/
    private static Task<IResult> GetDownloadUrl(string publicId, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }

    // TODO: Implement DeleteDocumentCommand in Application/Documents/Commands/DeleteDocument/
    private static Task<IResult> DeleteDocument(string publicId, CancellationToken ct)
    {
        return Task.FromResult(Results.StatusCode(StatusCodes.Status501NotImplemented));
    }
}
