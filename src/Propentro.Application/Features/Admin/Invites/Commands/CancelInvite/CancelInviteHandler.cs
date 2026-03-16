using Microsoft.EntityFrameworkCore;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Invites.Commands.CancelInvite;

public sealed class CancelInviteHandler(
    IAppDbContext dbContext,
    IClerkOrganizationService clerkService,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<CancelInviteCommand>
{
    public async Task<Result> Handle(CancelInviteCommand request, CancellationToken cancellationToken)
    {
        var invite = await dbContext.Invites
            .IgnoreQueryFilters()
            .Include(i => i.Organization)
            .FirstOrDefaultAsync(i => i.PublicId == request.Id, cancellationToken);

        if (invite is null)
            return InviteErrors.InviteNotFound;

        if (request.OrganizationId.HasValue && invite.OrganizationId != request.OrganizationId.Value)
            return InviteErrors.InviteNotFound;

        if (invite.Status != Invite.Statuses.Pending)
            return InviteErrors.NotCancellable;

        invite.Status = Invite.Statuses.Cancelled;
        invite.CancelledById = request.CancelledByUserId;
        invite.UpdatedAt = dateTimeProvider.UtcNow;

        // Revoke Clerk invitation if org has ExternalId
        if (!string.IsNullOrEmpty(invite.Organization.ExternalId))
        {
            await clerkService.RevokeInvitationAsync(
                invite.Organization.ExternalId, invite.Email, cancellationToken);
        }

        Log.Information("Cancelled invite {InvitePublicId} for {Email}",
            invite.PublicId, invite.Email);

        return Result.Success();
    }
}
