using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users.Commands.RemoveMember;

public sealed class RemoveMemberHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.OrganizationId, cancellationToken);

        if (org is null)
            return AdminUserErrors.OrgNotFound;

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId, cancellationToken);

        if (user is null)
            return AdminUserErrors.UserNotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return AdminUserErrors.NotAMember;

        // Hard delete OrganizationUserRoles for this user+org
        var roles = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == org.Id && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        dbContext.OrganizationUserRoles.RemoveRange(roles);

        // Hard delete OrganizationUsers membership
        dbContext.OrganizationUsers.Remove(membership);

        // Clerk sync — remove member if both external IDs exist
        if (org.ExternalId is not null && user.ExternalId is not null)
        {
            var clerkResult = await clerkService.RemoveMemberAsync(
                org.ExternalId, user.ExternalId, cancellationToken);

            if (clerkResult.IsFailure)
                return AdminUserErrors.ClerkSyncFailed;
        }

        // Check if user has other org memberships; if not, deactivate
        var hasOtherOrgs = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                ou => ou.UserId == user.Id && ou.OrganizationId != org.Id,
                cancellationToken);

        if (!hasOtherOrgs)
        {
            user.IsActive = false;
        }

        // Invalidate permission cache for this user+org
        await permissionService.InvalidatePermissionsCacheAsync(user.Id, org.Id, cancellationToken);

        Log.Information(
            "Removed user {UserPublicId} from organization {OrgPublicId}",
            user.PublicId, org.PublicId);

        return Result.Success();
    }
}
