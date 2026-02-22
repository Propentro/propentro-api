namespace Propentro.Application.Features.Webhooks.Commands.SyncUser;

/// <summary>
/// Upserts a user by external ID. Auth-provider agnostic.
/// </summary>
public record SyncUserCommand(
    string ExternalId,
    string AuthProvider,
    string Email,
    string? FirstName,
    string? LastName
) : ICommand;
