namespace Propentro.Application.Features.Webhooks.Commands.SyncOrganization;

/// <summary>
/// Upserts an organization by external ID. Auth-provider agnostic.
/// </summary>
public record SyncOrganizationCommand(
    string ExternalId,
    string AuthProvider,
    string Name,
    string Slug,
    string? LogoUrl
) : ICommand;
