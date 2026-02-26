using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.BulkArchiveContacts;

public sealed class BulkArchiveContactsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache,
    IDateTimeProvider dateTime) : ICommandHandler<BulkArchiveContactsCommand>
{
    public async Task<Result> Handle(BulkArchiveContactsCommand request, CancellationToken cancellationToken)
    {
        var contacts = await dbContext.Contacts
            .Where(c => request.ContactIds.Contains(c.PublicId))
            .ToListAsync(cancellationToken);

        foreach (var contact in contacts)
        {
            if (contact.IsArchived)
                continue;

            contact.IsArchived = true;
            contact.ArchivedAt = dateTime.UtcNow;
            contact.ArchivedById = currentUser.UserId;

            var activity = new Activity("contact", contact.Id, "archived")
            {
                Title = "Contact archived",
                Description = $"Contact \"{contact.DisplayName}\" was archived by {currentUser.FullName}.",
            };
            dbContext.Activities.Add(activity);

            contact.MarkAsArchived(currentUser.OrganizationId);
        }

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
