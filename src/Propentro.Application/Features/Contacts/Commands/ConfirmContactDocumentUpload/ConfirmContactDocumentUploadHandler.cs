using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts.Commands.ConfirmContactDocumentUpload;

public sealed class ConfirmContactDocumentUploadHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IBlobStorageService blobStorage,
    ICacheService cache) : ICommandHandler<ConfirmContactDocumentUploadCommand>
{
    public async Task<Result> Handle(
        ConfirmContactDocumentUploadCommand request,
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
        var isLinked = await dbContext.ContactDocuments
            .AnyAsync(cd => cd.ContactId == contact.Id && cd.DocumentId == document.Id, cancellationToken);

        if (!isLinked)
            return ContactDocumentErrors.DocumentNotLinked;

        // Find pending version
        var version = await dbContext.DocumentVersions
            .FirstOrDefaultAsync(
                v => v.DocumentId == document.Id && v.Status == "pending",
                cancellationToken);

        if (version is null)
            return ContactDocumentErrors.NoPendingVersion;

        // Verify file exists in R2
        var exists = await blobStorage.ExistsAsync(version.StorageKey, cancellationToken);

        if (!exists)
            return ContactDocumentErrors.UploadNotCompleted;

        // Confirm upload
        version.Status = "active";
        document.CurrentVersionId = version.Id;

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
