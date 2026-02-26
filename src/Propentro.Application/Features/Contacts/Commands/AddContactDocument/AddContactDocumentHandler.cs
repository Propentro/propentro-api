using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts.Commands.AddContactDocument;

public sealed class AddContactDocumentHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IBlobStorageService blobStorage,
    ICacheService cache) : ICommandHandler<AddContactDocumentCommand, string>
{
    public async Task<Result<string>> Handle(AddContactDocumentCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        // Upload file to blob storage
        var storageKey = await blobStorage.UploadAsync(
            request.FileContent,
            request.FileName,
            request.ContentType,
            cancellationToken);

        // Create Document
        var document = new Document(request.Name, request.DocumentType)
        {
            Description = request.Description,
            UploadedById = currentUser.UserId
        };
        dbContext.Documents.Add(document);

        // Create DocumentVersion
        var version = new DocumentVersion
        {
            Id = Guid.CreateVersion7(),
            DocumentId = document.Id,
            OriginalName = request.FileName,
            MimeType = request.ContentType,
            FileSize = request.FileSize,
            StorageKey = storageKey,
            VersionNumber = 1,
            Status = "uploaded",
            UploadedById = currentUser.UserId,
            CreatedAt = DateTimeOffset.UtcNow
        };
        dbContext.DocumentVersions.Add(version);
        document.CurrentVersionId = version.Id;

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

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return document.PublicId;
    }
}
