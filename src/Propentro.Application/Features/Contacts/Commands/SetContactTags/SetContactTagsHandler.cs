using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Tags;
using Activity = Propentro.Domain.Entities.Activities.Activity;

namespace Propentro.Application.Features.Contacts.Commands.SetContactTags;

public sealed class SetContactTagsHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    ICacheService cache) : ICommandHandler<SetContactTagsCommand>
{
    public async Task<Result> Handle(SetContactTagsCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        // Resolve desired tags — all must exist
        var desiredTagIds = request.TagIds.Distinct().ToArray();

        var tags = desiredTagIds.Length > 0
            ? await dbContext.Tags
                .Where(t => desiredTagIds.Contains(t.PublicId))
                .ToListAsync(cancellationToken)
            : [];

        if (tags.Count != desiredTagIds.Length)
        {
            var foundPublicIds = tags.Select(t => t.PublicId).ToHashSet();
            var missingId = desiredTagIds.First(id => !foundPublicIds.Contains(id));
            return TagErrors.NotFoundById(missingId);
        }

        var desiredInternalIds = tags.Select(t => t.Id).ToHashSet();

        // Load current assignments
        var currentAssignments = await dbContext.EntityTags
            .Where(et => et.EntityType == "contact" && et.EntityId == contact.Id)
            .ToListAsync(cancellationToken);

        var currentInternalIds = currentAssignments.Select(et => et.TagId).ToHashSet();

        // Compute diff
        var toRemove = currentAssignments.Where(et => !desiredInternalIds.Contains(et.TagId)).ToList();
        var toAdd = desiredInternalIds.Where(id => !currentInternalIds.Contains(id)).ToList();

        if (toRemove.Count == 0 && toAdd.Count == 0)
            return Result.Success();

        foreach (var entityTag in toRemove)
            dbContext.EntityTags.Remove(entityTag);

        foreach (var tagId in toAdd)
        {
            dbContext.EntityTags.Add(new EntityTag
            {
                TagId = tagId,
                EntityType = "contact",
                EntityId = contact.Id,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedById = currentUser.UserId
            });
        }

        var activity = new Activity("contact", contact.Id, "system_event")
        {
            Title = "Tags updated"
        };
        dbContext.Activities.Add(activity);

        await cache.RemoveByPatternAsync($"org:{currentUser.OrganizationId}:contacts:*", cancellationToken);

        return Result.Success();
    }
}
