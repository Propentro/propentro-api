using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Activities.Commands.UpdateActivity;

public sealed class UpdateActivityHandler(
    IAppDbContext dbContext) : ICommandHandler<UpdateActivityCommand>
{
    private static readonly HashSet<string> _userEditableTypes = ["note", "phone_call"];

    public async Task<Result> Handle(UpdateActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await dbContext.Activities
            .FirstOrDefaultAsync(a => a.PublicId == request.ActivityId, cancellationToken);

        if (activity is null)
            return ActivityErrors.NotFoundById(request.ActivityId);

        if (!_userEditableTypes.Contains(activity.ActivityType))
            return ActivityErrors.SystemActivityImmutable;

        if (request.Description is not null)
            activity.Description = request.Description;

        if (request.IsPinned.HasValue)
            activity.IsPinned = request.IsPinned.Value;

        return Result.Success();
    }
}
