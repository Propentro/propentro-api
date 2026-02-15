using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.UnarchiveContact;

public sealed class UnarchiveContactHandler(
    IAppDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<UnarchiveContactCommand>
{
    public async Task<Result> Handle(UnarchiveContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        if (!contact.IsArchived)
            return ContactErrors.NotArchived;

        contact.IsArchived = false;
        contact.ArchivedAt = null;
        contact.ArchivedById = null;

        dbContext.Activities.Add(new Activity("contact", contact.Id, "unarchived")
        {
            Title = "Contact unarchived",
            OrganizationId = currentUser.OrganizationId,
            CreatedById = currentUser.UserId
        });

        return Result.Success();
    }
}
