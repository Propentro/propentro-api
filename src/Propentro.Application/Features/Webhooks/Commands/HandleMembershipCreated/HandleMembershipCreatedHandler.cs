using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Models;
using Propentro.Domain.Common;
using Propentro.Domain.Entities.Identity.Events;

namespace Propentro.Application.Features.Webhooks.Commands.HandleMembershipCreated;

public sealed class HandleMembershipCreatedHandler(
    IAppDbContext dbContext,
    IDateTimeProvider dateTimeProvider,
    ICacheService cache) : ICommandHandler<HandleMembershipCreatedCommand>
{
    public async Task<Result> Handle(HandleMembershipCreatedCommand request, CancellationToken cancellationToken)
    {
        const string authProvider = "clerk";

        // 1. Upsert user by external ID
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.UserExternalId && u.AuthProvider == authProvider,
                cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.CreateVersion7(),
                PublicId = PublicIdGenerator.Generate(User.PublicIdPrefix),
                ExternalId = request.UserExternalId,
                AuthProvider = authProvider,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true
            };

            dbContext.Users.Add(user);

            Log.Information("Created user {PublicId} from membership webhook for {ExternalId}",
                user.PublicId, request.UserExternalId);
        }
        else
        {
            // Update email/name if changed
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            if (!user.IsActive)
                user.IsActive = true;
        }

        // 2. Resolve organization by Clerk external ID
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                o => o.ExternalId == request.ClerkOrganizationId && o.IsActive,
                cancellationToken);

        if (org is null)
        {
            Log.Warning("Organization not found for Clerk org {ClerkOrgId} — skipping membership",
                request.ClerkOrganizationId);
            return Result.Success();
        }

        // 3. Check if membership already exists (idempotency)
        var existingMembership = await dbContext.OrganizationUsers
            .IgnoreQueryFilters()
            .AnyAsync(
                ou => ou.OrganizationId == org.Id && ou.UserId == user.Id,
                cancellationToken);

        if (existingMembership)
        {
            Log.Information("Membership already exists for user {UserPublicId} in org {OrgPublicId} — skipping",
                user.PublicId, org.PublicId);
            return Result.Success();
        }

        // 4. Find pending invite for this email + org to get role assignment
        var normalizedEmail = request.Email.ToLowerInvariant();
        var pendingInvite = await dbContext.Invites
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                i => i.Email == normalizedEmail
                     && i.OrganizationId == org.Id
                     && i.Status == Invite.Statuses.Pending
                     && (!i.ExpiresAt.HasValue || i.ExpiresAt > dateTimeProvider.UtcNow),
                cancellationToken);

        if (pendingInvite is null)
        {
            // User was added to Clerk org outside the invite flow (e.g., directly via Clerk dashboard).
            // We cannot assign a role without an invite, so we skip local membership creation.
            Log.Warning(
                "No pending invite found for {Email} in org {OrgPublicId} — membership webhook received without matching invite",
                request.Email, org.PublicId);
            return Result.Success();
        }

        // 5. Create membership
        dbContext.OrganizationUsers.Add(new OrganizationUser
        {
            OrganizationId = org.Id,
            UserId = user.Id,
            Status = OrganizationUser.Statuses.Active
        });

        // 6. Assign role from invite
        dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
        {
            OrganizationId = org.Id,
            UserId = user.Id,
            RoleId = pendingInvite.RoleId,
            AssignedAt = dateTimeProvider.UtcNow,
            AssignedBy = pendingInvite.InvitedById
        });

        // 7. Mark invite as accepted
        pendingInvite.Status = Invite.Statuses.Accepted;

        Log.Information(
            "Created membership for user {UserPublicId} in org {OrgPublicId} via invite {InvitePublicId}",
            user.PublicId, org.PublicId, pendingInvite.PublicId);

        dbContext.AddOutboxEvent(new InviteAcceptedEvent(pendingInvite.Id, org.Id));

        await cache.RemoveByPatternAsync($"org:{org.Id}:user-permissions:*", cancellationToken);

        return Result.Success();
    }
}
