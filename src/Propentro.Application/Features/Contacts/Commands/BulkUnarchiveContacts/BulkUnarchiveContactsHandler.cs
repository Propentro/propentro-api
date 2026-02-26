using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.BulkUnarchiveContacts;

public sealed class BulkUnarchiveContactsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<BulkUnarchiveContactsCommand>
{
    public async Task<Result> Handle(BulkUnarchiveContactsCommand request, CancellationToken cancellationToken)
    {
        var contacts = await dbContext.Contacts
            .Where(c => request.ContactIds.Contains(c.PublicId))
            .ToListAsync(cancellationToken);

        foreach (var contact in contacts)
        {
            if (!contact.IsArchived)
                continue;

            contact.IsArchived = false;
            contact.ArchivedAt = null;
            contact.ArchivedById = null;

            var activity = new Activity("contact", contact.Id, "unarchived")
            {
                Title = "Contact unarchived",
                Description = $"Contact \"{contact.DisplayName}\" was unarchived by {currentUser.FullName}.",
            };
            dbContext.Activities.Add(activity);

            contact.MarkAsUnarchived(currentUser.OrganizationId);
        }

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
