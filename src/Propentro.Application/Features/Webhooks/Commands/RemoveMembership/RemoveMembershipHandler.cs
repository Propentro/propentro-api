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
            // Org must exist for membership removal — throw so Clerk retries
            throw new InvalidOperationException(
                $"Organization not found for external ID '{request.ExternalOrgId}'. " +
                "This may be a race condition — Clerk will retry.");
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalUserId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            // User must exist for membership removal — throw so Clerk retries
            throw new InvalidOperationException(
                $"User not found for external ID '{request.ExternalUserId}'. " +
                "This may be a race condition — Clerk will retry.");
        }

        // Remove role assignments
        var roles = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .Where(our => our.OrganizationId == org.Id && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
            dbContext.OrganizationUserRoles.Remove(role);

        // Remove membership
        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
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
