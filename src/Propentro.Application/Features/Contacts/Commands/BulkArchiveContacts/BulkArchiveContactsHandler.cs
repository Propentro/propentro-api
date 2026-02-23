using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.BulkArchiveContacts;

public sealed class BulkArchiveContactsHandler(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IDateTimeProvider dateTime) : ICommandHandler<BulkArchiveContactsCommand>
{
    public async Task<Result> Handle(BulkArchiveContactsCommand request, CancellationToken cancellationToken)
    {
        var contacts = await dbContext.Contacts
            .Where(c => request.ContactIds.Contains(c.PublicId))
            .ToListAsync(cancellationToken);

        if (contacts.Count == 0)
            return ContactErrors.NotFound;

        var now = dateTime.UtcNow;

        foreach (var contact in contacts.Where(c => !c.IsArchived))
        {
            contact.IsArchived = true;
            contact.ArchivedAt = now;
            contact.ArchivedById = currentUser.UserId;

            dbContext.Activities.Add(new Activity("contact", contact.Id, "archived")
            {
                Title = "Contact archived"
            });
        }

        return Result.Success();
    }
}
