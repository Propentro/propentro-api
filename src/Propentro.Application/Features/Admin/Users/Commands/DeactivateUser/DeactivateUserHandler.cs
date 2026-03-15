using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateUser;

public sealed class DeactivateUserHandler(
    IAppDbContext dbContext) : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.PublicId, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        if (!user.IsActive)
            return AdminUserErrors.AlreadyInactive;

        user.IsActive = false;

        Log.Information("Deactivated user {PublicId} ({Email})", user.PublicId, user.Email);

        return Result.Success();
    }
}
