using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles.Commands.UpdateRole;

public sealed class UpdateRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IPermissionService permissionService) : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.PublicId == request.RoleId && r.IsActive, cancellationToken);

        if (role is null)
            return RoleErrors.NotFound;

        if (role.IsSystem)
            return RoleErrors.CannotModifySystemRole;

        if (role.OrganizationId != currentUser.OrganizationId)
            return RoleErrors.NotFound;

        if (request.Name is not null && request.Name != role.Name)
        {
            var nameExists = await dbContext.Roles
                .IgnoreQueryFilters()
                .AnyAsync(
                    r => r.OrganizationId == currentUser.OrganizationId
                        && r.Name == request.Name
                        && r.IsActive
                        && r.Id != role.Id,
                    cancellationToken);

            if (nameExists)
                return RoleErrors.NameAlreadyExists;

            role.Name = request.Name;
        }

        if (request.Description is not null)
            role.Description = request.Description;

        if (request.PermissionCodes is not null)
        {
            // Remove old RolePermissions
            var existingPermissions = await dbContext.RolePermissions
                .Where(rp => rp.RoleId == role.Id)
                .ToListAsync(cancellationToken);

            foreach (var existing in existingPermissions)
                dbContext.RolePermissions.Remove(existing);

            // Add new RolePermissions
            if (request.PermissionCodes.Length > 0)
            {
                var permissions = await dbContext.Permissions
                    .Where(p => request.PermissionCodes.Contains(p.Code))
                    .ToListAsync(cancellationToken);

                foreach (var permission in permissions)
                {
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = role.Id,
                        PermissionId = permission.Id,
                        CreatedAt = DateTimeOffset.UtcNow
                    });
                }
            }
        }

        await permissionService.InvalidateOrganizationPermissionsCacheAsync(currentUser.OrganizationId, cancellationToken);

        return Result.Success();
    }
}
