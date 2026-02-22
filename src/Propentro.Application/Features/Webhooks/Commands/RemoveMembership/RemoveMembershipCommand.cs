using Propentro.Application.Common.Interfaces.Messaging;

namespace Propentro.Application.Features.Webhooks.Commands.RemoveMembership;

/// <summary>
/// Removes an organization membership. Auth-provider agnostic.
/// </summary>
public record RemoveMembershipCommand(
    string ExternalOrgId,
    string ExternalUserId,
    string AuthProvider
) : ICommand;
