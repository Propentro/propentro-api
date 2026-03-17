using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Application.Features.Admin.Invites;
using Propentro.Contracts.Organizations.Invites;
using Propentro.Domain.Common;

namespace Propentro.Application.Features.Organizations.Invites.Commands.InviteToCurrentOrg;

public sealed class InviteToCurrentOrgHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<InviteToCurrentOrgCommand, InviteResult>
{
    public async Task<Result<InviteResult>> Handle(InviteToCurrentOrgCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve organization by internal Id
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId && o.IsActive, cancellationToken);

        if (org is null)
            return InviteErrors.OrganizationNotFound;

        if (string.IsNullOrEmpty(org.ExternalId))
            return Error.Validation("Organization.ClerkNotLinked", "Organization is not linked to Clerk. Cannot send invite.");

        var orgExternalId = org.ExternalId;

        // 2. Resolve role by PublicId scoped to OrganizationId
        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.PublicId == request.RoleId
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
            return await HandleExistingUserAsync(org, orgExternalId, role, user, request, cancellationToken);
        }

        return await HandleNewUserAsync(org, orgExternalId, role, request, cancellationToken);
    }

    private async Task<Result<InviteResult>> HandleExistingUserAsync(
        Organization org,
        string orgExternalId,
        Role role,
        User user,
        InviteToCurrentOrgCommand request,
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

        // Sync to Clerk if user has external ID
        if (!string.IsNullOrEmpty(user.ExternalId))
        {
            var clerkResult = await clerkService.AddMemberAsync(
                orgExternalId, user.ExternalId, "basic_member", cancellationToken);

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
            "Invited existing user {UserPublicId} to organization {OrgPublicId} with role {RolePublicId}",
            user.PublicId, org.PublicId, role.PublicId);

        return new InviteResult(true, MemberPublicId: user.PublicId);
    }

    private async Task<Result<InviteResult>> HandleNewUserAsync(
        Organization org,
        string orgExternalId,
        Role role,
        InviteToCurrentOrgCommand request,
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

        // Send Clerk invitation (orgExternalId already validated non-null above)
        var clerkResult = await clerkService.InviteMemberAsync(
            orgExternalId, request.Email, "org:organization_viewer", cancellationToken);

        if (clerkResult.IsFailure)
            return InviteErrors.ClerkSyncFailed;

        // Create Invite record as Pending
        var invite = new Invite
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
        };
        dbContext.Invites.Add(invite);

        Log.Information(
            "Created pending invite for {Email} to organization {OrgPublicId}",
            request.Email, org.PublicId);

        return new InviteResult(false, InvitePublicId: invite.PublicId);
    }
}
