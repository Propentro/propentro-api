using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Organizations.Members;

namespace Propentro.Application.Features.Admin.Users.Commands.ReactivateMember;

public sealed class AdminReactivateMemberHandler(
    IAppDbContext dbContext,
    IPermissionService permissionService) : ICommandHandler<AdminReactivateMemberCommand>
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

        await permissionService.InvalidatePermissionsCacheAsync(user.Id, org.Id, cancellationToken);

        Log.Information(
            "Admin reactivated member {UserPublicId} in organisation {OrgPublicId}",
            user.PublicId, org.PublicId);

        return Result.Success();
    }
}
