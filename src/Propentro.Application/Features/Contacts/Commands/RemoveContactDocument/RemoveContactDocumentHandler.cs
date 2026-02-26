using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.RemoveContactDocument;

public sealed class RemoveContactDocumentHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache,
    IBlobStorageService blobStorage) : ICommandHandler<RemoveContactDocumentCommand>
{
    public async Task<Result> Handle(RemoveContactDocumentCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        var document = await dbContext.Documents
            .FirstOrDefaultAsync(d => d.PublicId == request.DocumentId, cancellationToken);

        if (document is null)
            return ContactDocumentErrors.DocumentNotFoundById(request.DocumentId);

        var contactDocument = await dbContext.ContactDocuments
            .FirstOrDefaultAsync(cd => cd.ContactId == contact.Id && cd.DocumentId == document.Id, cancellationToken);

        if (contactDocument is null)
            return ContactDocumentErrors.DocumentNotLinked;

        dbContext.ContactDocuments.Remove(contactDocument);

        // Activity log
        var activity = new Activity("contact", contact.Id, "system_event")
        {
            Title = "Document removed",
            Description = $"Removed document: {document.Name}"
        };
        dbContext.Activities.Add(activity);

        // Soft delete document if no other links exist
        var otherLinks = await dbContext.ContactDocuments
            .AnyAsync(cd => cd.DocumentId == document.Id && cd.Id != contactDocument.Id, cancellationToken);

        if (!otherLinks)
        {
            List<string> storageKeys = await dbContext.DocumentVersions
                .Where(v => v.DocumentId == document.Id)
                .Select(v => v.StorageKey)
                .ToListAsync(cancellationToken);

            foreach (var key in storageKeys)
            {
                bool deleted = await blobStorage.DeleteAsync(key, cancellationToken);

                if (!deleted)
                    Log.Warning("Failed to delete blob {StorageKey} for document {DocumentId}", key, request.DocumentId);
            }

            document.IsActive = false;
        }

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
