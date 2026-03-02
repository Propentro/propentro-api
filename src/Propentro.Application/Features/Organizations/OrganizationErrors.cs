using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations;

public static class OrganizationErrors
{
    public static readonly Error NotFound = new(
        "Organization.NotFound",
        "Organization not found.",
        ErrorType.NotFound);

    public static readonly Error ClerkSyncFailed = new(
        "Organization.ClerkSyncFailed",
        "Failed to sync organization name with the auth provider. Please try again.",
        ErrorType.Failure);
}
