using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Users.Commands.DeactivateMember;

public sealed class AdminDeactivateMemberHandler(
    IAppDbContext dbContext,
    ICacheService cache) : ICommandHandler<AdminDeactivateMemberCommand>
{
    public async Task<Result> Handle(AdminDeactivateMemberCommand request, CancellationToken cancellationToken)
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

        if (membership.Status == OrganizationUser.Statuses.Deactivated)
            return MemberErrors.AlreadyDeactivated;

        membership.Status = OrganizationUser.Statuses.Deactivated;

        var hasOtherActiveMemberships = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(ou => ou.UserId == user.Id
                && ou.OrganizationId != org.Id
                && ou.Status == OrganizationUser.Statuses.Active,
                cancellationToken);

        if (!hasOtherActiveMemberships)
            user.IsActive = false;

        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);

        Log.Information(
            "Admin deactivated member {UserPublicId} in organization {OrgPublicId}",
            user.PublicId, org.PublicId);

        dbContext.AddOutboxEvent(new MemberDeactivatedEvent(user.Id, org.Id));

        return Result.Success();
    }
}
