using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Organizations.Commands.DeactivateOrganization;

public sealed class DeactivateOrganizationHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<DeactivateOrganizationCommand>
{
    public async Task<Result> Handle(DeactivateOrganizationCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.Id, cancellationToken);

        if (org is null)
            return OrganizationErrors.NotFound;

        if (!org.IsActive)
            return AdminOrganizationErrors.AlreadyInactive;

        // Clerk sync FIRST — delete the org from Clerk before modifying local data
        if (org.ExternalId is not null)
        {
            var clerkResult = await clerkService.DeleteOrganizationAsync(org.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                return OrganizationErrors.SyncFailed;
        }

        // Load memberships to get user IDs
        var memberships = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .Where(ou => ou.OrganizationId == org.Id)
            .ToListAsync(cancellationToken);

        var memberUserIds = memberships.Select(ou => ou.UserId).ToList();

        // Hard delete OrganizationUserRoles for this org
        var roles = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == org.Id)
            .ToListAsync(cancellationToken);

        dbContext.OrganizationUserRoles.RemoveRange(roles);

        // Hard delete OrganizationUsers for this org
        dbContext.OrganizationUsers.RemoveRange(memberships);

        // Cancel pending invites
        var pendingInvites = await dbContext.Invites
            .Where(i => i.OrganizationId == org.Id && i.Status == Invite.Statuses.Pending)
            .ToListAsync(cancellationToken);

        foreach (var invite in pendingInvites)
        {
            invite.Status = Invite.Statuses.Canceled;
        }

        // Deactivate the organization
        org.IsActive = false;

        // For each member user, check if they have other orgs. If not, deactivate the user.
        if (memberUserIds.Count > 0)
        {
            var usersWithOtherOrgs = await dbContext.OrganizationUsers
                .IgnoreQueryFilters()
                .Where(ou => memberUserIds.Contains(ou.UserId) && ou.OrganizationId != org.Id)
                .Select(ou => ou.UserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            var orphanedUserIds = memberUserIds
                .Except(usersWithOtherOrgs)
                .ToList();

            if (orphanedUserIds.Count > 0)
            {
                var orphanedUsers = await dbContext.Users
                    .IgnoreQueryFilters()
                    .Where(u => orphanedUserIds.Contains(u.Id))
                    .ToListAsync(cancellationToken);

                foreach (var user in orphanedUsers)
                {
                    user.IsActive = false;
                }
            }
        }

        // Invalidate permission cache for all members of this org
        await permissionService.InvalidateOrganizationPermissionsCacheAsync(org.Id, cancellationToken);

        Log.Information("Deactivated organization {PublicId} ({Name}) with {MemberCount} members",
            org.PublicId, org.Name, memberUserIds.Count);

        dbContext.AddOutboxEvent(new OrganizationDeactivatedEvent(org.Id, org.Id, memberUserIds));

        return Result.Success();
    }
}
