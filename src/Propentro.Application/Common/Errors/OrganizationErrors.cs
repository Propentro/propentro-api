using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Errors;

public static class OrganizationErrors
{
    public static readonly Error NotLinkedToAuthProvider = Error.ExternalService(
        "Organization.NotLinkedToAuthProvider",
        "Organization is not linked to the authentication provider. Please contact support.");
}
