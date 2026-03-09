using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles.Commands.DeleteRole;

public sealed class DeleteRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IPermissionService permissionService) : ICommandHandler<DeleteRoleCommand>
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

        // Check no active members have this role
        var activeMemberCount = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .CountAsync(
                our => our.OrganizationId == currentUser.OrganizationId && our.RoleId == role.Id,
                cancellationToken);

        if (activeMemberCount > 0)
            return RoleErrors.HasActiveMembers;

        // Hard delete RolePermissions
        var rolePermissions = await dbContext.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        foreach (var rp in rolePermissions)
            dbContext.RolePermissions.Remove(rp);

        // Soft delete role
        role.IsActive = false;

        await permissionService.InvalidateOrganizationPermissionsCacheAsync(currentUser.OrganizationId, cancellationToken);

        return Result.Success();
    }
}
