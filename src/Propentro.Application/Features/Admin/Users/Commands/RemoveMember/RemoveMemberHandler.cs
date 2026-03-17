using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Users.Commands.RemoveMember;

public sealed class RemoveMemberHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    ICacheService cache) : ICommandHandler<RemoveMemberCommand>
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

        var roles = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == org.Id && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        dbContext.OrganizationUserRoles.RemoveRange(roles);
        dbContext.OrganizationUsers.Remove(membership);

        if (org.ExternalId is not null && user.ExternalId is not null)
        {
            var clerkResult = await clerkService.RemoveMemberAsync(
                org.ExternalId, user.ExternalId, cancellationToken);

            if (clerkResult.IsFailure)
                return AdminUserErrors.SyncFailed;
        }

        var hasOtherOrgs = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                ou => ou.UserId == user.Id && ou.OrganizationId != org.Id,
                cancellationToken);

        if (!hasOtherOrgs)
        {
            user.IsActive = false;
        }

        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);

        Log.Information(
            "Removed user {UserPublicId} from organization {OrgPublicId}",
            user.PublicId, org.PublicId);

        dbContext.AddOutboxEvent(new MemberRemovedEvent(user.Id, org.Id));

        return Result.Success();
    }
}
