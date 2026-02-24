using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Webhooks.Commands.RemoveMembership;

/// <summary>
/// Removes an organization membership and associated role assignments. Idempotent.
/// Invalidates permission cache after removal.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class RemoveMembershipHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<RemoveMembershipCommand>
{
    public async Task<Result> Handle(RemoveMembershipCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.ExternalId == request.ExternalOrgId && o.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (org is null)
        {
            Log.Warning("Cannot remove membership: organization not found for external ID {ExternalOrgId}",
                request.ExternalOrgId);
            return Result.Success();
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalUserId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            Log.Warning("Cannot remove membership: user not found for external ID {ExternalUserId}",
                request.ExternalUserId);
            return Result.Success();
        }

        // Remove role assignments
        var roles = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == org.Id && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
            dbContext.OrganizationUserRoles.Remove(role);

        // Remove membership
        var membership = await dbContext.OrganizationUsers
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (membership is not null)
        {
            dbContext.OrganizationUsers.Remove(membership);
            Log.Information("Removed membership for user {UserPublicId} from org {OrgPublicId}",
                user.PublicId, org.PublicId);
        }
        else
        {
            Log.Debug("Membership already removed for user {UserPublicId} in org {OrgPublicId}",
                user.PublicId, org.PublicId);
        }

        // Invalidate permission cache after membership removal
        await permissionService.InvalidatePermissionsCacheAsync(user.Id, org.Id, cancellationToken);

        return Result.Success();
    }
}
