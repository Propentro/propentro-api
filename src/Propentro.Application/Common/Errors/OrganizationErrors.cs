using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Errors;

public static class OrganizationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Organization.NotFound", "Organization not found.");

    public static readonly Error SyncFailed = Error.ExternalService(
        "Organization.SyncFailed", "Failed to sync organization. Please try again.");

    public static readonly Error NotLinkedToAuthProvider = Error.ExternalService(
        "Organization.NotLinkedToAuthProvider",
        "Organization is not linked to the authentication provider. Please contact support.");
}
