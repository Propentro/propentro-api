using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activity;

namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public sealed class CreateActivityHandler(
    IAppDbContext dbContext) : ICommandHandler<CreateActivityCommand, string>
{
    public async Task<Result<string>> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ActivityErrors.ContactNotFound;

        var activity = new Activity("contact", contact.Id, request.ActivityType)
        {
            Title = request.Title,
            Description = request.Description
        };

        dbContext.Activities.Add(activity);

        return activity.PublicId;
    }
}
