using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;
using Propentro.Domain.Common;

namespace Propentro.Application.Features.Webhooks.Commands.SyncUser;

/// <summary>
/// Upserts a user by (external_id, auth_provider). Idempotent.
/// After upsert, matches any pending invites for this user's email.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class SyncUserHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<SyncUserCommand>
{
    public async Task<Result> Handle(SyncUserCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                u => u.ExternalId == request.ExternalId && u.AuthProvider == request.AuthProvider,
                cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.CreateVersion7(),
                PublicId = PublicIdGenerator.Generate(User.PublicIdPrefix),
                ExternalId = request.ExternalId,
                AuthProvider = request.AuthProvider,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true
            };

            dbContext.Users.Add(user);

            Log.Information("Created user {PublicId} for external ID {ExternalId}",
                user.PublicId, request.ExternalId);
        }
        else
        {
            user.Email = request.Email;
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;

            Log.Information("Updated user {PublicId} for external ID {ExternalId}",
                user.PublicId, request.ExternalId);
        }

        // Match pending invites for this user's email
        await MatchPendingInvitesAsync(user, cancellationToken);

        return Result.Success();
    }

    private async Task MatchPendingInvitesAsync(User user, CancellationToken cancellationToken)
    {
        var pendingInvites = await dbContext.Invites
            .IgnoreQueryFilters()
            .Include(i => i.Organization)
            .Where(i => string.Equals(i.Email, user.Email, StringComparison.OrdinalIgnoreCase)
                && i.Status == Invite.Statuses.Pending
                && (!i.ExpiresAt.HasValue || i.ExpiresAt > dateTimeProvider.UtcNow))
            .ToListAsync(cancellationToken);

        foreach (var invite in pendingInvites)
        {
            dbContext.OrganizationUsers.Add(new OrganizationUser
            {
                OrganizationId = invite.OrganizationId,
                UserId = user.Id,
                Status = OrganizationUser.Statuses.Active
            });

            dbContext.OrganizationUserRoles.Add(new OrganizationUserRole
            {
                OrganizationId = invite.OrganizationId,
                UserId = user.Id,
                RoleId = invite.RoleId,
                AssignedAt = dateTimeProvider.UtcNow,
                AssignedBy = invite.InvitedById
            });

            if (!string.IsNullOrEmpty(invite.Organization.ExternalId) && !string.IsNullOrEmpty(user.ExternalId))
            {
                await clerkService.AddMemberAsync(
                    invite.Organization.ExternalId, user.ExternalId, "basic_member", cancellationToken);
            }

            invite.Status = Invite.Statuses.Accepted;

            Log.Information("Matched invite {InvitePublicId} for user {UserPublicId} in org {OrgPublicId}",
                invite.PublicId, user.PublicId, invite.Organization.PublicId);
        }

        if (pendingInvites.Count > 0)
        {
            Log.Information("Matched {Count} pending invites for user {Email}",
                pendingInvites.Count, user.Email);
        }
    }
}
