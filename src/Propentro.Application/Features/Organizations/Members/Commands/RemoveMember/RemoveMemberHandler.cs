using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.Commands.RemoveMember;

public sealed class RemoveMemberHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IPermissionService permissionService) : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
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

        // Guard: if user is an Owner, check org has ≥2 owners
        var ownerRole = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == "Owner" && r.IsSystem && r.IsActive, cancellationToken);

        if (ownerRole is not null)
        {
            var userIsOwner = await dbContext.OrganizationUserRoles
                .IgnoreQueryFilters()
                .AnyAsync(
                    our => our.OrganizationId == currentUser.OrganizationId
                        && our.UserId == user.Id
                        && our.RoleId == ownerRole.Id,
                    cancellationToken);

            if (userIsOwner)
            {
                var ownerCount = await dbContext.OrganizationUserRoles
                    .IgnoreQueryFilters()
                    .CountAsync(
                        our => our.OrganizationId == currentUser.OrganizationId
                            && our.RoleId == ownerRole.Id,
                        cancellationToken);

                if (ownerCount <= 1)
                    return MemberErrors.CannotRemoveLastOwner;
            }
        }

        // Remove all roles
        var existingRoles = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .Where(our => our.OrganizationId == currentUser.OrganizationId && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var role in existingRoles)
            dbContext.OrganizationUserRoles.Remove(role);

        // Remove membership
        dbContext.OrganizationUsers.Remove(membership);

        // Outbox event for email notification
        dbContext.AddOutboxEvent(new MembershipRemovedEvent(user.Id, currentUser.OrganizationId));

        await permissionService.InvalidatePermissionsCacheAsync(user.Id, currentUser.OrganizationId, cancellationToken);

        return Result.Success();
    }
}
