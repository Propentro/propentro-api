using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.BulkUnarchiveContacts;

public sealed class BulkUnarchiveContactsHandler(
    IAppDbContext dbContext) : ICommandHandler<BulkUnarchiveContactsCommand>
{
    public async Task<Result> Handle(BulkUnarchiveContactsCommand request, CancellationToken cancellationToken)
    {
        var contacts = await dbContext.Contacts
            .Where(c => request.ContactIds.Contains(c.PublicId))
            .ToListAsync(cancellationToken);

        if (contacts.Count == 0)
            return ContactErrors.NotFound;

        foreach (var contact in contacts)
        {
            if (!contact.IsArchived)
                continue;

            contact.IsArchived = false;
            contact.ArchivedAt = null;
            contact.ArchivedById = null;

            dbContext.Activities.Add(new Activity("contact", contact.Id, "unarchived")
            {
                Title = "Contact unarchived"
            });
        }

        return Result.Success();
    }
}
