using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.Commands.ReactivateMember;

public sealed class ReactivateMemberHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IClerkOrganizationService clerkService) : ICommandHandler<ReactivateMemberCommand>
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

        if (membership.Status == OrganizationUser.Statuses.Active)
            return MemberErrors.AlreadyActive;

        membership.Status = OrganizationUser.Statuses.Active;
        user.IsActive = true;

        var org = await dbContext.Organizations
            .FirstOrDefaultAsync(o => o.Id == currentUser.OrganizationId, cancellationToken);

        if (org?.ExternalId is not null && user.ExternalId is not null)
        {
            var clerkResult = await clerkService.AddMemberAsync(org.ExternalId, user.ExternalId, cancellationToken);
            if (clerkResult.IsFailure)
                Log.Warning("Failed to re-add user {UserId} to Clerk org — member reactivated in DB but Clerk sync pending", user.PublicId);
        }

        dbContext.AddOutboxEvent(new MemberReactivatedEvent(user.Id, currentUser.OrganizationId));

        return Result.Success();
    }
}
