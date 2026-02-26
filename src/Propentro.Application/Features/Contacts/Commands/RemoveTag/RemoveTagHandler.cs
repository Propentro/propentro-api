using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Tags;

namespace Propentro.Application.Features.Contacts.Commands.RemoveTag;

public sealed class RemoveTagHandler(
    IAppDbContext dbContext) : ICommandHandler<RemoveTagCommand>
{
    public async Task<Result> Handle(RemoveTagCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        var tag = await dbContext.Tags
            .FirstOrDefaultAsync(t => t.PublicId == request.TagId, cancellationToken);

        if (tag is null)
            return TagErrors.NotFoundById(request.TagId);

        var entityTag = await dbContext.EntityTags
            .FirstOrDefaultAsync(et => et.TagId == tag.Id && et.EntityType == "contact" && et.EntityId == contact.Id, cancellationToken);

        if (entityTag is null)
            return TagErrors.TagNotAssigned;

        dbContext.EntityTags.Remove(entityTag);

        return Result.Success();
    }
}
