using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateUser;

public sealed class ReactivateUserHandler(
    IAppDbContext dbContext) : ICommandHandler<ReactivateUserCommand>
{
    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.Id, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        if (user.IsActive)
            return AdminUserErrors.AlreadyActive;

        user.IsActive = true;

        Log.Information("Reactivated user {PublicId} ({Email})", user.PublicId, user.Email);

        return Result.Success();
    }
}
