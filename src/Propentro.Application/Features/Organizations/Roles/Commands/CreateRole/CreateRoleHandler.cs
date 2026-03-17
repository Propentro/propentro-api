using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles.Commands.CreateRole;

public sealed class CreateRoleHandler(
    IAppDbContext dbContext,
    IUserContext currentUser,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CreateRoleCommand, string>
{
    public async Task<Result<string>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await dbContext.Roles
            .IgnoreQueryFilters()
            .AnyAsync(
                r => r.OrganizationId == currentUser.OrganizationId
                    && r.Name == request.Name
                    && r.IsActive,
                cancellationToken);

        if (nameExists)
            return RoleErrors.NameAlreadyExists;

        var role = new Role
        {
            Id = Guid.CreateVersion7(),
            PublicId = Domain.Common.PublicIdGenerator.Generate(Role.PublicIdPrefix),
            OrganizationId = currentUser.OrganizationId,
            Name = request.Name,
            Description = request.Description,
            IsSystem = false
        };

        dbContext.Roles.Add(role);

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
                    CreatedAt = dateTimeProvider.UtcNow
                });
            }
        }

        return role.PublicId;
    }
}
