using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.BulkUntagContacts;

public sealed class BulkUntagContactsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<BulkUntagContactsCommand>
{
    public async Task<Result> Handle(BulkUntagContactsCommand request, CancellationToken cancellationToken)
    {
        // Resolve tags — skip missing silently (removal of nonexistent = no-op)
        var distinctTagIds = request.TagIds.Distinct().ToArray();

        var tags = await dbContext.Tags
            .Where(t => distinctTagIds.Contains(t.PublicId))
            .ToListAsync(cancellationToken);

        if (tags.Count == 0)
            return Result.Success();

        var tagInternalIds = tags.Select(t => t.Id).ToHashSet();

        // Resolve contacts — skip missing silently
        var distinctContactIds = request.ContactIds.Distinct().ToArray();

        var contacts = await dbContext.Contacts
            .Where(c => distinctContactIds.Contains(c.PublicId))
            .ToListAsync(cancellationToken);

        if (contacts.Count == 0)
            return Result.Success();

        var contactInternalIds = contacts.Select(c => c.Id).ToHashSet();

        // Load matching assignments in one query
        var assignmentsToRemove = await dbContext.EntityTags
            .Where(et => et.EntityType == "contact"
                         && contactInternalIds.Contains(et.EntityId)
                         && tagInternalIds.Contains(et.TagId))
            .ToListAsync(cancellationToken);

        var assignmentsByContact = assignmentsToRemove
            .GroupBy(et => et.EntityId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var contact in contacts)
        {
            if (!assignmentsByContact.TryGetValue(contact.Id, out var assignments))
                continue;

            foreach (var entityTag in assignments)
                dbContext.EntityTags.Remove(entityTag);

            var activity = new Activity("contact", contact.Id, "system_event")
            {
                Title = "Tags removed"
            };
            dbContext.Activities.Add(activity);
        }

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
