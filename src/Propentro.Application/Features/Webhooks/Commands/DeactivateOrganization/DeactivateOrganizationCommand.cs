namespace Propentro.Application.Features.Webhooks.Commands.DeactivateOrganization;

/// <summary>
/// Soft-deletes an organization by external ID. Auth-provider agnostic.
/// </summary>
public record DeactivateOrganizationCommand(
    string ExternalId,
    string AuthProvider
) : ICommand;
