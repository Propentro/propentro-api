using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Members.Commands.DeactivateMember;

public sealed class DeactivateMemberHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<DeactivateMemberCommand>
{
    public async Task<Result> Handle(DeactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId && u.IsActive, cancellationToken);

        if (user is null)
            return MemberErrors.NotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == currentUser.OrganizationId && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return MemberErrors.UserNotMember;

        if (membership.Status == OrganizationUser.Statuses.Deactivated)
            return MemberErrors.AlreadyDeactivated;

        if (user.Id == currentUser.UserId)
            return MemberErrors.CannotDeactivateSelf;

        // Guard: cannot deactivate the last owner
        var ownerRole = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "Owner" && r.IsSystem && r.IsActive, cancellationToken);

        if (ownerRole is not null)
        {
            var userIsOwner = await dbContext.OrganizationUserRoles
                .IgnoreQueryFilters()
                .AnyAsync(our => our.OrganizationId == currentUser.OrganizationId
                    && our.UserId == user.Id
                    && our.RoleId == ownerRole.Id,
                    cancellationToken);

            if (userIsOwner)
            {
                var ownerCount = await dbContext.OrganizationUserRoles
                    .IgnoreQueryFilters()
                    .CountAsync(our => our.OrganizationId == currentUser.OrganizationId
                        && our.RoleId == ownerRole.Id,
                        cancellationToken);

                if (ownerCount <= 1)
                    return MemberErrors.CannotRemoveLastOwner;
            }
        }

        // Remove from Clerk org first — if Clerk fails, roll back entirely
        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org?.ExternalId is not null && user.ExternalId is not null)
        {
            var clerkResult = await clerkService.RemoveMemberAsync(org.ExternalId, user.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
            {
                Log.Warning("Failed to remove user {UserId} from Clerk org — aborting deactivation", user.PublicId);
                return MemberErrors.SyncFailed;
            }
        }

        membership.Status = OrganizationUser.Statuses.Deactivated;

        // If user has no other active memberships, deactivate the user account
        var hasOtherActiveMemberships = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(ou => ou.UserId == user.Id
                && ou.OrganizationId != currentUser.OrganizationId
                && ou.Status == OrganizationUser.Statuses.Active,
                cancellationToken);

        if (!hasOtherActiveMemberships)
            user.IsActive = false;

        await permissionService.InvalidatePermissionsCacheAsync(user.Id, currentUser.OrganizationId, cancellationToken);

        return Result.Success();
    }
}
