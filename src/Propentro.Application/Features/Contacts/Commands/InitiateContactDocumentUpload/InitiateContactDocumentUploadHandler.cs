using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Documents;

namespace Propentro.Application.Features.Contacts.Commands.InitiateContactDocumentUpload;

public sealed class InitiateContactDocumentUploadHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IBlobStorageService blobStorage,
    ICacheService cache) : ICommandHandler<InitiateContactDocumentUploadCommand, ContactDocumentUploadResponse>
{
    public async Task<Result<ContactDocumentUploadResponse>> Handle(
        InitiateContactDocumentUploadCommand request,
        CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        // Create Document
        var document = new Document(request.Name, request.DocumentType)
        {
            Description = request.Description,
            UploadedById = currentUser.UserId
        };
        dbContext.Documents.Add(document);

        // Create DocumentVersion with pending status
        var version = new DocumentVersion(document.Id, request.FileName, request.ContentType, request.FileSize)
        {
            VersionNumber = 1,
            Status = "pending",
            UploadedById = currentUser.UserId
        };

        // Storage key: {org.PublicId}/{doc.PublicId}/{docv.PublicId}
        string storageKey = $"{currentUser.PublicOrganizationId}/{document.PublicId}/{version.PublicId}";
        version.StorageKey = storageKey;

        dbContext.DocumentVersions.Add(version);

        // Create ContactDocument link
        var contactDocument = new ContactDocument
        {
            Id = Guid.CreateVersion7(),
            ContactId = contact.Id,
            DocumentId = document.Id,
            IsPrimary = request.IsPrimary,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.ContactDocuments.Add(contactDocument);

        // Generate presigned upload URL (15 minutes)
        string uploadUrl = await blobStorage.GetPresignedUploadUrlAsync(
            storageKey,
            request.ContentType,
            TimeSpan.FromMinutes(15),
            cancellationToken);

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return new ContactDocumentUploadResponse(document.PublicId, uploadUrl);
    }
}
