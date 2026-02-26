using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts.Commands.UpdateContactDocument;

public sealed class UpdateContactDocumentHandler(
    IAppDbContext dbContext) : ICommandHandler<UpdateContactDocumentCommand>
{
    public async Task<Result> Handle(UpdateContactDocumentCommand request, CancellationToken cancellationToken)
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

        if (request.Name is not null)
            document.Name = request.Name;

        if (request.Description is not null)
            document.Description = request.Description;

        if (request.IsPrimary.HasValue)
            contactDocument.IsPrimary = request.IsPrimary.Value;

        return Result.Success();
    }
}
