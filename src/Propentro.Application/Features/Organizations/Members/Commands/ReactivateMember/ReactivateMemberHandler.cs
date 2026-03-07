using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Members.Commands.ReactivateMember;

public sealed class ReactivateMemberHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkService,
    IPermissionService permissionService) : ICommandHandler<ReactivateMemberCommand>
{
    public async Task<Result> Handle(ReactivateMemberCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.PublicId == request.UserId, cancellationToken);

        if (user is null)
            return MemberErrors.NotFound;

        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == currentUser.OrganizationId && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
            return MemberErrors.UserNotMember;

        if (membership.Status == "active")
            return MemberErrors.AlreadyActive;

        membership.Status = "active";
        user.IsActive = true;

        // Re-add to Clerk org using their existing Clerk ID (direct membership, no invite email)
        // Graceful degradation: if Clerk fails, log warning but proceed — admin can re-invite manually
        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org?.ExternalId is not null && user.ExternalId is not null)
        {
            var userRole = await dbContext.OrganizationUserRoles
                .IgnoreQueryFilters()
                .Include(our => our.Role)
                .Where(our => our.OrganizationId == currentUser.OrganizationId && our.UserId == user.Id)
                .FirstOrDefaultAsync(cancellationToken);

            string clerkRole = userRole?.Role.Name switch
            {
                "Owner" => "org:organization_owner",
                "Manager" => "org:organization_manager",
                _ => "org:organization_viewer"
            };

            var clerkResult = await clerkService.AddMemberAsync(org.ExternalId, user.ExternalId, clerkRole, cancellationToken);
            if (clerkResult.IsFailure)
                Log.Warning("Failed to re-add user {UserId} to Clerk org — member reactivated in DB but Clerk sync pending", user.PublicId);
        }

        await permissionService.InvalidatePermissionsCacheAsync(user.Id, currentUser.OrganizationId, cancellationToken);

        return Result.Success();
    }
}
