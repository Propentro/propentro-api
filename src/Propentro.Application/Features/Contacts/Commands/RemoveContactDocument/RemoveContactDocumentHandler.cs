using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts.Commands.RemoveContactDocument;

public sealed class RemoveContactDocumentHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<RemoveContactDocumentCommand>
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

        // Soft delete document if no other links exist
        var otherLinks = await dbContext.ContactDocuments
            .AnyAsync(cd => cd.DocumentId == document.Id && cd.Id != contactDocument.Id, cancellationToken);

        if (!otherLinks)
            document.IsActive = false;

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
