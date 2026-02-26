using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Tags;

namespace Propentro.Application.Features.Contacts.Commands.AssignTag;

public sealed class AssignTagHandler(
    IAppDbContext dbContext,
    IUserContext currentUser) : ICommandHandler<AssignTagCommand>
{
    public async Task<Result> Handle(AssignTagCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        var tag = await dbContext.Tags
            .FirstOrDefaultAsync(t => t.PublicId == request.TagId, cancellationToken);

        if (tag is null)
            return TagErrors.NotFoundById(request.TagId);

        var alreadyAssigned = await dbContext.EntityTags
            .AnyAsync(et => et.TagId == tag.Id && et.EntityType == "contact" && et.EntityId == contact.Id, cancellationToken);

        if (alreadyAssigned)
            return TagErrors.TagAlreadyAssigned;

        var entityTag = new EntityTag
        {
            TagId = tag.Id,
            EntityType = "contact",
            EntityId = contact.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedById = currentUser.UserId
        };

        dbContext.EntityTags.Add(entityTag);

        return Result.Success();
    }
}
