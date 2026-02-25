using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Models;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Webhooks.Commands.SyncMembership;

/// <summary>
/// Upserts an organization membership with role mapping. Idempotent.
/// Resolves org and user by external IDs, then upserts organization_users
/// and organization_user_roles records.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class SyncMembershipHandler(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    IPermissionService permissionService) : ICommandHandler<SyncMembershipCommand>
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
            // Org may not be committed yet (race condition) — throw so Clerk retries
            throw new InvalidOperationException(
                $"Organization not found for external ID '{request.ExternalOrgId}'. " +
                "This may be a race condition — Clerk will retry.");
        }

        if (!org.IsActive)
        {
            Log.Warning("Skipping membership sync: organization {OrgPublicId} is deactivated", org.PublicId);
            return Result.Success();
        }

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalUserId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            // User may not be committed yet (race condition) — throw so Clerk retries
            throw new InvalidOperationException(
                $"User not found for external ID '{request.ExternalUserId}'. " +
                "This may be a race condition — Clerk will retry.");
        }

        if (!user.IsActive)
        {
            Log.Warning("Skipping membership sync: user {UserPublicId} is deactivated", user.PublicId);
            return Result.Success();
        }

        // Upsert organization_users
        var membership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
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
            dbContext.AddOutboxEvent(new MembershipCreatedEvent(user.Id, org.Id));

            Log.Information("Created membership for user {UserPublicId} in org {OrgPublicId}",
                user.PublicId, org.PublicId);
        }
        else
        {
            membership.Status = "active";
        }

        // Map Clerk role to system role
        string systemRoleName = MapClerkRole(request.Role);
        bool roleChanged = await SyncRoleAsync(org.Id, user.Id, systemRoleName, cancellationToken);

        // Invalidate permission cache when role changes
        if (roleChanged)
            await permissionService.InvalidatePermissionsCacheAsync(user.Id, org.Id, cancellationToken);

        return Result.Success();
    }

    private async Task<bool> SyncRoleAsync(
        Guid orgId, Guid userId, string roleName, CancellationToken ct)
    {
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(r => r.Name == roleName && r.IsSystem, ct);

        if (role is null)
        {
            Log.Warning("System role {RoleName} not found, skipping role assignment", roleName);
            return false;
        }

        // Remove existing roles for this user in this org
        var existingRoles = await dbContext.OrganizationUserRoles
            .IgnoreQueryFilters()
            .Where(our => our.OrganizationId == orgId && our.UserId == userId)
            .ToListAsync(ct);

        // Check if the role is already assigned
        if (existingRoles.Any(r => r.RoleId == role.Id))
            return false;

        // Remove old roles and add the new one
        foreach (var existingRole in existingRoles)
            dbContext.OrganizationUserRoles.Remove(existingRole);

        dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
        {
            OrganizationId = orgId,
            UserId = userId,
            RoleId = role.Id,
            AssignedAt = dateTimeProvider.UtcNow
        });

        Log.Information("Assigned role {RoleName} to user {UserId} in org {OrgId}",
            roleName, userId, orgId);

        return true;
    }

    private static string MapClerkRole(string clerkRole) => clerkRole switch
    {
        "org:organisation_owner" => "Owner",
        "org:organisation_manager" => "Manager",
        "org:organisation_viewer" => "Viewer",
        _ => "Viewer" // Default to least privileged role
    };
}
