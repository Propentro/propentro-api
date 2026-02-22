using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities;
using Serilog;

namespace Propentro.Application.Features.Webhooks.Commands.SyncMembership;

/// <summary>
/// Upserts an organization membership with role mapping. Idempotent.
/// Resolves org and user by external IDs, then upserts organization_users
/// and organization_user_roles records.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class SyncMembershipHandler(IAppDbContext dbContext) : ICommandHandler<SyncMembershipCommand>
{
    public async Task<Result> Handle(SyncMembershipCommand request, CancellationToken cancellationToken)
    {
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.ExternalId == request.ExternalOrgId && o.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (org is null)
        {
            Log.Warning("Cannot sync membership: organization not found for external ID {ExternalOrgId}",
                request.ExternalOrgId);
            return Result.Success();
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalUserId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            Log.Warning("Cannot sync membership: user not found for external ID {ExternalUserId}",
                request.ExternalUserId);
            return Result.Success();
        }

        // Upsert organization_users
        var membership = await dbContext.OrganizationUsers
            .FirstOrDefaultAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (membership is null)
        {
            membership = new OrganizationUser
            {
                OrganizationId = org.Id,
                UserId = user.Id,
                Status = "active"
            };
            dbContext.OrganizationUsers.Add(membership);

            Log.Information("Created membership for user {UserPublicId} in org {OrgPublicId}",
                user.PublicId, org.PublicId);
        }
        else
        {
            membership.Status = "active";
        }

        // Map Clerk role to system role
        var systemRoleName = MapClerkRole(request.Role);
        await SyncRoleAsync(org.Id, user.Id, systemRoleName, cancellationToken);

        return Result.Success();
    }

    private async Task SyncRoleAsync(
        Guid orgId, Guid userId, string roleName, CancellationToken ct)
    {
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == roleName && r.IsSystem, ct);

        if (role is null)
        {
            Log.Warning("System role {RoleName} not found, skipping role assignment", roleName);
            return;
        }

        // Remove existing roles for this user in this org
        var existingRoles = await dbContext.OrganizationUserRoles
            .Where(our => our.OrganizationId == orgId && our.UserId == userId)
            .ToListAsync(ct);

        // Check if the role is already assigned
        if (existingRoles.Any(r => r.RoleId == role.Id))
            return;

        // Remove old roles and add the new one
        foreach (var existingRole in existingRoles)
            dbContext.OrganizationUserRoles.Remove(existingRole);

        dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
        {
            OrganizationId = orgId,
            UserId = userId,
            RoleId = role.Id,
            AssignedAt = DateTimeOffset.UtcNow
        });

        Log.Information("Assigned role {RoleName} to user {UserId} in org {OrgId}",
            roleName, userId, orgId);
    }

    private static string MapClerkRole(string clerkRole) => clerkRole switch
    {
        "org:admin" => "Owner",
        "org:member" => "Member",
        _ => "Member"
    };
}
