using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles.Commands.DeleteRole;

public sealed class DeleteRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IPermissionService permissionService,
    ICacheService cache,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.PublicId == request.RoleId && r.IsActive, cancellationToken);

        if (role is null || role.OrganizationId != currentUser.OrganizationId)
            return RoleErrors.NotFound;

        if (role.IsSystem)
            return RoleErrors.CannotModifySystemRole;

        // Find all members with this role
        var memberRoleAssignments = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .Where(our => our.OrganizationId == currentUser.OrganizationId && our.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        if (memberRoleAssignments.Count > 0)
        {
            // Find the Viewer system role
            var viewerRole = await dbContext.Roles
                .IgnoreQueryFilters()
                .FirstAsync(r => r.Name == "Viewer" && r.IsSystem && r.IsActive, cancellationToken);

            var affectedUserIds = memberRoleAssignments.Select(a => a.UserId).Distinct().ToList();

            var existingViewerAssignments = await dbContext.OrganizationUserRoles
                .IgnoreQueryFilters()
                .Where(our => our.OrganizationId == currentUser.OrganizationId
                    && our.RoleId == viewerRole.Id
                    && affectedUserIds.Contains(our.UserId))
                .Select(our => our.UserId)
                .ToListAsync(cancellationToken);

            var usersNeedingViewer = affectedUserIds.Except(existingViewerAssignments).ToList();

            foreach (var userId in usersNeedingViewer)
            {
                dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
                {
                    OrganizationId = currentUser.OrganizationId,
                    UserId = userId,
                    RoleId = viewerRole.Id,
                    AssignedAt = dateTimeProvider.UtcNow,
                    AssignedBy = currentUser.UserId
                });
            }

            foreach (var assignment in memberRoleAssignments)
                dbContext.OrganizationUserRoles.Remove(assignment);
        }

        // Hard delete RolePermissions
        var rolePermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        foreach (var rp in rolePermissions)
            dbContext.RolePermissions.Remove(rp);

        // Soft delete role
        role.IsActive = false;

        await permissionService.InvalidateOrganizationPermissionsCacheAsync(currentUser.OrganizationId, cancellationToken);
        await cache.RemoveAsync($"org:{currentUser.OrganizationId}:roles", cancellationToken);

        return Result.Success();
    }
}
