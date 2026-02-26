using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.UnarchiveContact;

public sealed class UnarchiveContactHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<UnarchiveContactCommand>
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

        // Activity log
        var activity = new Activity("contact", contact.Id, "unarchived")
        {
            Title = "Contact unarchived",
            Description = $"Contact \"{contact.DisplayName}\" was unarchived by {currentUser.FullName}.",
        };
        dbContext.Activities.Add(activity);

        // Domain event (outbox)
        contact.MarkAsUnarchived(currentUser.OrganizationId);

        // Cache invalidation
        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
