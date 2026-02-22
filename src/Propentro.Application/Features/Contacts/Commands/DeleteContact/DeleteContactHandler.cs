using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Contacts.Commands.DeleteContact;

public sealed class DeleteContactHandler(
    IAppDbContext dbContext) : ICommandHandler<DeleteContactCommand>
{
    public async Task<Result> Handle(DeleteContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        contact.IsActive = false;

        return Result.Success();
    }
}
