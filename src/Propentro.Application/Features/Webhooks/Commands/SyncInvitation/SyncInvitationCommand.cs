namespace Propentro.Application.Features.Webhooks.Commands.SyncInvitation;

/// <summary>
/// Tracks invitation state for audit purposes. Auth-provider agnostic.
/// </summary>
public record SyncInvitationCommand(
    string ExternalOrgId,
    string InviteeEmail,
    string AuthProvider,
    string Status
) : ICommand;
