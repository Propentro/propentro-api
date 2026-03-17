using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateUser;

public sealed class DeactivateUserHandler(
    IAppDbContext dbContext,
    ICacheService cache) : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.Id, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        if (!user.IsActive)
            return AdminUserErrors.AlreadyInactive;

        user.IsActive = false;

        Log.Information("Deactivated user {PublicId} ({Email})", user.PublicId, user.Email);

        // Invalidate permission cache for all organizations the user belongs to
        var orgIds = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .Where(ou => ou.UserId == user.Id)
            .Select(ou => ou.OrganizationId)
            .ToListAsync(cancellationToken);

        foreach (var orgId in orgIds)
        {
            await cache.RemoveByPatternAsync($"org:{orgId}:user-permissions:*", cancellationToken);
        }

        return Result.Success();
    }
}
