using Propentro.Application.Common.Interfaces.Messaging;

namespace Propentro.Application.Features.Webhooks.Commands.SyncMembership;

/// <summary>
/// Upserts an organization membership with role mapping. Auth-provider agnostic.
/// </summary>
public record SyncMembershipCommand(
    string ExternalOrgId,
    string ExternalUserId,
    string AuthProvider,
    string Role
) : ICommand;
