using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Activities.Commands.DeleteActivity;

public sealed class DeleteActivityHandler(
    IAppDbContext dbContext) : ICommandHandler<DeleteActivityCommand>
{
    private static readonly string[] _systemTypes = ["system_event", "archived", "unarchived"];

    public async Task<Result> Handle(DeleteActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await dbContext.Activities
            .FirstOrDefaultAsync(a => a.PublicId == request.ActivityId, cancellationToken);

        if (activity is null)
            return ActivityErrors.NotFoundById(request.ActivityId);

        if (_systemTypes.Contains(activity.ActivityType))
            return ActivityErrors.SystemActivityImmutable;

        activity.IsActive = false;

        return Result.Success();
    }
}
