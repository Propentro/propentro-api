using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Organizations.Members.Commands.UpdateMemberRoles;

public sealed class UpdateMemberRolesHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IDateTimeProvider dateTimeProvider,
    IPermissionService permissionService) : ICommandHandler<UpdateMemberRolesCommand>
{
    public async Task<Result> Handle(UpdateMemberRolesCommand request, CancellationToken cancellationToken)
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

        // Load roles by PublicIds — must be system roles OR org-specific roles
        var roles = await dbContext.Roles
            .IgnoreQueryFilters()
            .Where(r => request.RoleIds.Contains(r.PublicId)
                && r.IsActive
                && (r.IsSystem || r.OrganizationId == currentUser.OrganizationId))
            .ToListAsync(cancellationToken);

        if (roles.Count != request.RoleIds.Length)
            return MemberErrors.InvalidRoleIds;

        // Guard: if removing Owner role, check org still has ≥1 other Owner
        var ownerRole = roles.FirstOrDefault(r => r.Name == "Owner" && r.IsSystem);
        if (ownerRole is null)
        {
            // Removing the owner role — check if org has other owners
            var ownerSystemRole = await dbContext.Roles
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Name == "Owner" && r.IsSystem && r.IsActive, cancellationToken);

            if (ownerSystemRole is not null)
            {
                var ownerCount = await dbContext.OrganizationUserRoles
                    .IgnoreQueryFilters()
                    .CountAsync(
                        our => our.OrganizationId == currentUser.OrganizationId
                            && our.RoleId == ownerSystemRole.Id
                            && our.UserId != user.Id,
                        cancellationToken);

                var currentUserHasOwnerRole = await dbContext.OrganizationUserRoles
                    .IgnoreQueryFilters()
                    .AnyAsync(
                        our => our.OrganizationId == currentUser.OrganizationId
                            && our.UserId == user.Id
                            && our.RoleId == ownerSystemRole.Id,
                        cancellationToken);

                if (currentUserHasOwnerRole && ownerCount == 0)
                    return MemberErrors.CannotRemoveLastOwner;
            }
        }

        // Remove all existing roles
        var existingRoles = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .Where(our => our.OrganizationId == currentUser.OrganizationId && our.UserId == user.Id)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingRoles)
            dbContext.OrganizationUserRoles.Remove(existing);

        // Add new roles
        var now = dateTimeProvider.UtcNow;
        foreach (var role in roles)
        {
            dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
            {
                OrganizationId = currentUser.OrganizationId,
                UserId = user.Id,
                RoleId = role.Id,
                AssignedAt = now,
                AssignedBy = currentUser.UserId
            });
        }

        await permissionService.InvalidatePermissionsCacheAsync(user.Id, currentUser.OrganizationId, cancellationToken);

        dbContext.AddOutboxEvent(new MemberRolesUpdatedEvent(user.Id, currentUser.OrganizationId));

        return Result.Success();
    }
}
