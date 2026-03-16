using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Common;

namespace Propentro.Application.Features.Admin.Invites.Commands.InviteUser;

public sealed class InviteUserHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<InviteUserCommand>
{
    public async Task<Result> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve organization by PublicId
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.PublicId == request.OrganizationId && o.IsActive, cancellationToken);

        if (org is null)
            return InviteErrors.OrganizationNotFound;

        // 2. Resolve role by PublicId
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.PublicId == request.RolePublicId
                     && r.IsActive
                     && (r.IsSystem || r.OrganizationId == org.Id),
                cancellationToken);

        if (role is null)
            return InviteErrors.RoleNotFound;

        // 3. Check if user exists by email
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => string.Equals(u.Email, request.Email, StringComparison.OrdinalIgnoreCase),
                cancellationToken);

        if (user is not null)
        {
            return await HandleExistingUserAsync(org, role, user, request, cancellationToken);
        }

        return await HandleNewUserAsync(org, role, request, cancellationToken);
    }

    private async Task<Result> HandleExistingUserAsync(
        Organization org,
        Role role,
        User user,
        InviteUserCommand request,
        CancellationToken cancellationToken)
    {
        // Check not already a member
        var isMember = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (isMember)
            return InviteErrors.AlreadyMember;

        // If inactive user, reactivate
        if (!user.IsActive)
            user.IsActive = true;

        // Create OrganizationUser (Active)
        dbContext.OrganizationUsers.Add(new OrganizationUser
        {
            OrganizationId = org.Id,
            UserId = user.Id,
            Status = OrganizationUser.Statuses.Active
        });

        // Create OrganizationUserRole
        dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
        {
            OrganizationId = org.Id,
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAt = dateTimeProvider.UtcNow,
            AssignedBy = request.InvitedByUserId
        });

        // Sync to Clerk — org and user must be linked
        if (string.IsNullOrEmpty(org.ExternalId))
            return Error.Validation("Organization.ClerkNotLinked", "Organisation is not linked to Clerk. Cannot send invite.");

        if (!string.IsNullOrEmpty(user.ExternalId))
        {
            var clerkResult = await clerkService.AddMemberAsync(
                org.ExternalId, user.ExternalId, "basic_member", cancellationToken);

            if (clerkResult.IsFailure)
                return InviteErrors.ClerkSyncFailed;
        }

        // Create Invite record as Accepted
        dbContext.Invites.Add(new Invite
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Invite.PublicIdPrefix),
            OrganizationId = org.Id,
            Email = request.Email,
            RoleId = role.Id,
            Status = Invite.Statuses.Accepted,
            InvitedById = request.InvitedByUserId,
            CreatedAt = dateTimeProvider.UtcNow
        });

        Log.Information(
            "Invited existing user {UserPublicId} to organisation {OrgPublicId} with role {RolePublicId}",
            user.PublicId, org.PublicId, role.PublicId);

        return Result.Success();
    }

    private async Task<Result> HandleNewUserAsync(
        Organization org,
        Role role,
        InviteUserCommand request,
        CancellationToken cancellationToken)
    {
        // Check for existing pending invite (same email + org, case-insensitive)
        var hasPendingInvite = await dbContext.Invites
            .IgnoreQueryFilters()
            .AnyAsync(
                i => string.Equals(i.Email, request.Email, StringComparison.OrdinalIgnoreCase)
                     && i.OrganizationId == org.Id
                     && i.Status == Invite.Statuses.Pending,
                cancellationToken);

        if (hasPendingInvite)
            return InviteErrors.AlreadyInvited;

        // Send Clerk invitation — org must be linked to Clerk
        if (string.IsNullOrEmpty(org.ExternalId))
            return Error.Validation("Organization.ClerkNotLinked", "Organisation is not linked to Clerk. Cannot send invite.");

        var clerkResult = await clerkService.InviteMemberAsync(
            org.ExternalId, request.Email, "org:organization_viewer", cancellationToken);

        if (clerkResult.IsFailure)
            return InviteErrors.ClerkSyncFailed;

        // Create Invite record as Pending
        dbContext.Invites.Add(new Invite
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Invite.PublicIdPrefix),
            OrganizationId = org.Id,
            Email = request.Email,
            RoleId = role.Id,
            Status = Invite.Statuses.Pending,
            InvitedById = request.InvitedByUserId,
            ExpiresAt = dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = dateTimeProvider.UtcNow
        });

        Log.Information(
            "Created pending invite for {Email} to organisation {OrgPublicId}",
            request.Email, org.PublicId);

        return Result.Success();
    }
}
