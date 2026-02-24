using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Webhooks.Commands.SyncInvitation;

/// <summary>
/// Tracks invitation state changes for audit purposes. Idempotent.
/// Currently logs the invitation status — future versions may persist to a dedicated table.
/// Does NOT call SaveChangesAsync — UnitOfWorkBehavior handles persistence.
/// </summary>
public sealed class SyncInvitationHandler : ICommandHandler<SyncInvitationCommand>
{
    public Task<Result> Handle(SyncInvitationCommand request, CancellationToken cancellationToken)
    {
        Log.Information(
            "Organization invitation state change: org={ExternalOrgId}, email={InviteeEmail}, status={Status}, provider={AuthProvider}",
            request.ExternalOrgId, request.InviteeEmail, request.Status, request.AuthProvider);

        return Task.FromResult(Result.Success());
    }
}
