using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Activities.Commands.DeleteActivity;

public sealed class DeleteActivityHandler(
    IAppDbContext dbContext) : ICommandHandler<DeleteActivityCommand>
{
    private static readonly HashSet<string> _userDeletableTypes = ["note", "phone_call"];

    public async Task<Result> Handle(DeleteActivityCommand request, CancellationToken cancellationToken)
    {
        var activity = await dbContext.Activities
            .FirstOrDefaultAsync(a => a.PublicId == request.ActivityId, cancellationToken);

        if (activity is null)
            return ActivityErrors.NotFoundById(request.ActivityId);

        if (!_userDeletableTypes.Contains(activity.ActivityType))
            return ActivityErrors.SystemActivityImmutable;

        activity.IsActive = false;

        return Result.Success();
    }
}
