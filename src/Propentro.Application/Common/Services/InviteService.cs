using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Errors;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Invites;
using Propentro.Domain.Common;

namespace Propentro.Application.Common.Services;

public sealed class InviteService(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IDateTimeProvider dateTimeProvider) : IInviteService
{
    public async Task<Result<InviteResult>> InviteUserToOrganizationAsync(
        Organization org,
        string email,
        string rolePublicId,
        Guid invitedByUserId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(org.ExternalId))
            return OrganizationErrors.NotLinkedToAuthProvider;

        var role = await dbContext.Roles
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                r => r.PublicId == rolePublicId
                     && r.IsActive
                     && (r.IsSystem || r.OrganizationId == org.Id),
                cancellationToken);

        if (role is null)
            return InviteErrors.RoleNotFound;

        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase),
                cancellationToken);

        if (user is not null)
            return await HandleExistingUserAsync(org, role, user, invitedByUserId, email, cancellationToken);

        return await HandleNewUserAsync(org, role, invitedByUserId, email, cancellationToken);
    }

    private async Task<Result<InviteResult>> HandleExistingUserAsync(
        Organization org,
        Role role,
        User user,
        Guid invitedByUserId,
        string email,
        CancellationToken cancellationToken)
    {
        var isMember = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (isMember)
            return InviteErrors.AlreadyMember;

        if (!user.IsActive)
            user.IsActive = true;

        dbContext.OrganizationUsers.Add(new OrganizationUser
        {
            OrganizationId = org.Id,
            UserId = user.Id,
            Status = OrganizationUser.Statuses.Active
        });

        dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
        {
            OrganizationId = org.Id,
            UserId = user.Id,
            RoleId = role.Id,
            AssignedAt = dateTimeProvider.UtcNow,
            AssignedBy = invitedByUserId
        });

        if (!string.IsNullOrEmpty(user.ExternalId))
        {
            var clerkResult = await clerkService.AddMemberAsync(
                org.ExternalId!, user.ExternalId, cancellationToken);

            if (clerkResult.IsFailure)
                return InviteErrors.SyncFailed;
        }

        dbContext.Invites.Add(new Invite
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Invite.PublicIdPrefix),
            OrganizationId = org.Id,
            Email = email,
            RoleId = role.Id,
            Status = Invite.Statuses.Accepted,
            InvitedById = invitedByUserId,
            CreatedAt = dateTimeProvider.UtcNow
        });

        Log.Information(
            "Invited existing user {UserPublicId} to organization {OrgPublicId} with role {RolePublicId}",
            user.PublicId, org.PublicId, role.PublicId);

        return new InviteResult { IsDirectAdd = true, MemberId = user.PublicId };
    }

    private async Task<Result<InviteResult>> HandleNewUserAsync(
        Organization org,
        Role role,
        Guid invitedByUserId,
        string email,
        CancellationToken cancellationToken)
    {
        var hasPendingInvite = await dbContext.Invites
            .IgnoreQueryFilters()
            .AnyAsync(
                i => string.Equals(i.Email, email, StringComparison.OrdinalIgnoreCase)
                     && i.OrganizationId == org.Id
                     && i.Status == Invite.Statuses.Pending,
                cancellationToken);

        if (hasPendingInvite)
            return InviteErrors.AlreadyPending;

        var clerkResult = await clerkService.InviteMemberAsync(
            org.ExternalId!, email, cancellationToken);

        if (clerkResult.IsFailure)
            return InviteErrors.SyncFailed;

        var invite = new Invite
        {
            Id = Guid.CreateVersion7(),
            PublicId = PublicIdGenerator.Generate(Invite.PublicIdPrefix),
            OrganizationId = org.Id,
            Email = email,
            RoleId = role.Id,
            Status = Invite.Statuses.Pending,
            InvitedById = invitedByUserId,
            ExpiresAt = dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = dateTimeProvider.UtcNow
        };
        dbContext.Invites.Add(invite);

        Log.Information(
            "Created pending invite for {Email} to organization {OrgPublicId}",
            email, org.PublicId);

        return new InviteResult { IsDirectAdd = false, InviteId = invite.PublicId };
    }
}
