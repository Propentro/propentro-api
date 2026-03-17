using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Settings;

public static class OrganizationSettingsErrors
{
    public static readonly Error NotFound = new(
        "Organization.NotFound",
        "Organization not found.",
        ErrorType.NotFound);

    public static readonly Error SyncFailed = Error.ExternalService(
        "Organization.SyncFailed",
        "Failed to sync organization settings. Please try again.");
}
