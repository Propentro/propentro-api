using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.OrganizationUsers)
            .FirstOrDefaultAsync(u => u.PublicId == request.Id, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        var organizationIds = user.OrganizationUsers
            .Select(ou => ou.OrganizationId)
            .ToList();

        foreach (var orgId in organizationIds)
        {
            await cache.RemoveByPatternAsync($"org:{orgId}:user-permissions:*", cancellationToken);
        }

        if (user.ExternalId is not null)
        {
            try
            {
                await clerkService.DeleteUserAsync(user.ExternalId, cancellationToken);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete user {ExternalId} from Clerk during GDPR hard delete — proceeding with DB deletion", user.ExternalId);
            }
        }

        dbContext.Users.Remove(user);

        Log.Information("Hard deleted user {PublicId} ({Email}) — GDPR request", user.PublicId, user.Email);

        return Result.Success();
    }
}
