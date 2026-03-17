using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;

public sealed class AdminReactivateMemberHandler(
    IAppDbContext dbContext,
    ICacheService cache) : ICommandHandler<AdminReactivateMemberCommand>
{
    public async Task<Result> Handle(AdminReactivateMemberCommand request, CancellationToken cancellationToken)
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

        if (membership.Status == OrganizationUser.Statuses.Active)
            return MemberErrors.AlreadyActive;

        membership.Status = OrganizationUser.Statuses.Active;
        user.IsActive = true;

        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);

        Log.Information(
            "Admin reactivated member {UserPublicId} in organization {OrgPublicId}",
            user.PublicId, org.PublicId);

        dbContext.AddOutboxEvent(new MemberReactivatedEvent(user.Id, org.Id));

        return Result.Success();
    }
}
