using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Time;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;

namespace Propentro.Application.Features.Contacts.Commands.ArchiveContact;

public sealed class ArchiveContactHandler(
    IAppDbContext dbContext,
    ICurrentUser currentUser,
    IDateTimeProvider dateTime) : ICommandHandler<ArchiveContactCommand>
{
    public async Task<Result> Handle(ArchiveContactCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ContactErrors.NotFoundById(request.ContactId);

        if (contact.IsArchived)
            return ContactErrors.AlreadyArchived;

        contact.IsArchived = true;
        contact.ArchivedAt = dateTime.UtcNow;
        contact.ArchivedById = currentUser.UserId;

        dbContext.Activities.Add(new Activity("contact", contact.Id, "archived")
        {
            Title = "Contact archived",
            OrganizationId = currentUser.OrganizationId,
            CreatedById = currentUser.UserId
        });

        return Result.Success();
    }
}
