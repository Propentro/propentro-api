using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.DeleteUser;

public sealed class DeleteUserHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<DeleteUserCommand>
{
    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .Include(u => u.OrganizationUsers)
            .FirstOrDefaultAsync(u => u.PublicId == request.Id, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        // Capture org IDs before removal for cache invalidation
        var organizationIds = user.OrganizationUsers
            .Select(ou => ou.OrganizationId)
            .ToList();

        // Invalidate permission caches for all orgs this user belongs to
        foreach (var orgId in organizationIds)
        {
            await permissionService.InvalidatePermissionsCacheAsync(user.Id, orgId, cancellationToken);
        }

        // GDPR: Clerk deletion is best-effort — DB deletion takes priority
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

        // Hard delete the user (FK SET NULL handled by DB cascade rules)
        dbContext.Users.Remove(user);

        Log.Information("Hard deleted user {PublicId} ({Email}) — GDPR request", user.PublicId, user.Email);

        return Result.Success();
    }
}
