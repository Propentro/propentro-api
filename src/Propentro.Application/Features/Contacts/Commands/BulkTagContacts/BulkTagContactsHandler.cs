using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Tags;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.BulkTagContacts;

public sealed class BulkTagContactsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<BulkTagContactsCommand>
{
    public async Task<Result> Handle(BulkTagContactsCommand request, CancellationToken cancellationToken)
    {
        // Resolve tags — all must exist
        var distinctTagIds = request.TagIds.Distinct().ToArray();

        var tags = await dbContext.Tags
            .Where(t => distinctTagIds.Contains(t.PublicId))
            .ToListAsync(cancellationToken);

        if (tags.Count != distinctTagIds.Length)
        {
            var foundPublicIds = tags.Select(t => t.PublicId).ToHashSet();
            var missingId = distinctTagIds.First(id => !foundPublicIds.Contains(id));
            return TagErrors.NotFoundById(missingId);
        }

        var tagInternalIds = tags.Select(t => t.Id).ToHashSet();

        // Resolve contacts — skip missing silently
        var distinctContactIds = request.ContactIds.Distinct().ToArray();

        var contacts = await dbContext.Contacts
            .Where(c => distinctContactIds.Contains(c.PublicId))
            .ToListAsync(cancellationToken);

        if (contacts.Count == 0)
            return Result.Success();

        var contactInternalIds = contacts.Select(c => c.Id).ToHashSet();

        // Load existing assignments in one query
        var existingAssignments = await dbContext.EntityTags
            .Where(et => et.EntityType == "contact"
                         && contactInternalIds.Contains(et.EntityId)
                         && tagInternalIds.Contains(et.TagId))
            .ToListAsync(cancellationToken);

        var existingPairs = existingAssignments
            .Select(et => (et.EntityId, et.TagId))
            .ToHashSet();

        foreach (var contact in contacts)
        {
            var added = false;

            foreach (var tag in tags)
            {
                if (existingPairs.Contains((contact.Id, tag.Id)))
                    continue;

                dbContext.EntityTags.Add(new EntityTag
                {
                    TagId = tag.Id,
                    EntityType = "contact",
                    EntityId = contact.Id,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedById = currentUser.UserId
                });

                added = true;
            }

            if (added)
            {
                var activity = new Activity("contact", contact.Id, "system_event")
                {
                    Title = "Tags added"
                };
                dbContext.Activities.Add(activity);
            }
        }

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
