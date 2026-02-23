using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Serilog;

namespace Propentro.Application.Features.Webhooks.Commands.DeactivateUser;

/// <summary>
/// Soft-deletes a user by (external_id, auth_provider). Idempotent.
/// Cascades to disable all memberships and remove role assignments.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class DeactivateUserHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<DeactivateUserCommand>
{
    public async Task<Result> Handle(DeactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            Log.Warning("Cannot deactivate user: no user found for external ID {ExternalId}",
                request.ExternalId);
            return Result.Success();
        }

        if (!user.IsActive)
        {
            Log.Debug("User {PublicId} is already deactivated", user.PublicId);
            return Result.Success();
        }

        // 1. Deactivate user
        user.IsActive = false;

        // 2. Disable all memberships
        var memberships = await dbContext.OrganizationUsers
            .Where(ou => ou.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            membership.Status = "disabled";
        }

        // 3. Remove all role assignments
        var roleAssignments = await dbContext.OrganizationUserRoles
            .Where(our => our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var role in roleAssignments)
        {
            dbContext.OrganizationUserRoles.Remove(role);
        }

        // 4. Invalidate permission caches for all affected orgs
        var affectedOrgIds = memberships.Select(m => m.OrganizationId).Distinct();
        foreach (var orgId in affectedOrgIds)
        {
            await permissionService.InvalidatePermissionsCacheAsync(user.Id, orgId, cancellationToken);
        }

        Log.Information(
            "Deactivated user {PublicId} — disabled {MembershipCount} memberships, removed {RoleCount} role assignments",
            user.PublicId, memberships.Count, roleAssignments.Count);

        return Result.Success();
    }
}
