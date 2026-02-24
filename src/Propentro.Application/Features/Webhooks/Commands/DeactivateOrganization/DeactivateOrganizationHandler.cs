using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Webhooks.Commands.DeactivateOrganization;

/// <summary>
/// Soft-deletes an organization by (external_id, auth_provider). Idempotent.
/// Cascades to disable all memberships and remove role assignments.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class DeactivateOrganizationHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<DeactivateOrganizationCommand>
{
    public async Task<Result> Handle(DeactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.ExternalId == request.ExternalId && o.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (org is null)
        {
            Log.Warning("Cannot deactivate organization: not found for external ID {ExternalId}",
                request.ExternalId);
            return Result.Success();
        }

        if (!org.IsActive)
        {
            Log.Debug("Organization {PublicId} is already deactivated", org.PublicId);
            return Result.Success();
        }

        // 1. Deactivate organization
        org.IsActive = false;

        // 2. Disable all memberships in this organization
        var memberships = await dbContext.OrganizationUsers
            .Where(ou => ou.OrganizationId == org.Id)
            .ToListAsync(cancellationToken);

        foreach (var membership in memberships)
        {
            membership.Status = "disabled";
        }

        // 3. Remove all role assignments in this organization
        var roleAssignments = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == org.Id)
            .ToListAsync(cancellationToken);

        foreach (var role in roleAssignments)
        {
            dbContext.OrganizationUserRoles.Remove(role);
        }

        // 4. Invalidate permission cache for entire organization
        await permissionService.InvalidateOrganizationPermissionsCacheAsync(org.Id, cancellationToken);

        Log.Information(
            "Deactivated organization {PublicId} — disabled {MembershipCount} memberships, removed {RoleCount} roles",
            org.PublicId, memberships.Count, roleAssignments.Count);

        return Result.Success();
    }
}
