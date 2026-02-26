using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Documents;

namespace Propentro.Application.Features.Contacts.Commands.InitiateContactDocumentVersionUpload;

public sealed class InitiateContactDocumentVersionUploadHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IBlobStorageService blobStorage,
    ICacheService cache) : ICommandHandler<InitiateContactDocumentVersionUploadCommand, ContactDocumentUploadResponse>
{
    public async Task<Result<ContactDocumentUploadResponse>> Handle(
        InitiateContactDocumentVersionUploadCommand request,
        CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        var document = await dbContext.Documents
            .FirstOrDefaultAsync(d => d.PublicId == request.DocumentId, cancellationToken);

        if (document is null)
            return ContactDocumentErrors.DocumentNotFoundById(request.DocumentId);

        // Verify document is linked to this contact
        bool isLinked = await dbContext.ContactDocuments
            .AnyAsync(cd => cd.ContactId == contact.Id && cd.DocumentId == document.Id, cancellationToken);

        if (!isLinked)
            return ContactDocumentErrors.DocumentNotLinked;

        // Get the latest version number
        int latestVersionNumber = await dbContext.DocumentVersions
            .Where(v => v.DocumentId == document.Id)
            .MaxAsync(v => (int?)v.VersionNumber, cancellationToken) ?? 0;

        // Create new DocumentVersion with pending status
        var version = new DocumentVersion(document.Id, request.FileName, request.ContentType, request.FileSize)
        {
            VersionNumber = latestVersionNumber + 1,
            Status = "pending",
            UploadedById = currentUser.UserId
        };

        // Storage key: {org.PublicId}/{doc.PublicId}/{docv.PublicId}
        string storageKey = $"{currentUser.PublicOrganizationId}/{document.PublicId}/{version.PublicId}";
        version.StorageKey = storageKey;

        dbContext.DocumentVersions.Add(version);

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
