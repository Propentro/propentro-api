using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Activity = Propentro.Domain.Entities.Activity;

namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public sealed class CreateActivityHandler(
    IAppDbContext dbContext,
    ICurrentUser currentUser) : ICommandHandler<CreateActivityCommand, string>
{
    private static readonly HashSet<string> UserCreatableTypes = ["note", "phone_call"];

    public async Task<Result<string>> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        if (!UserCreatableTypes.Contains(request.ActivityType))
            return ActivityErrors.InvalidActivityType;

        var contact = await dbContext.Contacts
            .FirstOrDefaultAsync(c => c.PublicId == request.ContactId, cancellationToken);

        if (contact is null)
            return ActivityErrors.ContactNotFound;

        var activity = new Activity("contact", contact.Id, request.ActivityType)
        {
            Title = request.Title,
            Description = request.Description,
            OrganizationId = currentUser.OrganizationId,
            CreatedById = currentUser.UserId
        };

        dbContext.Activities.Add(activity);

        return activity.PublicId;
    }
}
