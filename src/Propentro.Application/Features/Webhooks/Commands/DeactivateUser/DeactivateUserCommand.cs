using Propentro.Application.Common.Interfaces.Messaging;

namespace Propentro.Application.Features.Webhooks.Commands.DeactivateUser;

/// <summary>
/// Soft-deletes a user by external ID. Auth-provider agnostic.
/// </summary>
public record DeactivateUserCommand(
    string ExternalId,
    string AuthProvider
) : ICommand;
