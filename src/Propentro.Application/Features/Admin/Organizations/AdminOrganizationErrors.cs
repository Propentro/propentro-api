using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations;

public static class AdminOrganizationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "Organization.NotFound", "Organization not found.");

    public static readonly Error SlugTaken = Error.Conflict(
        "Organization.SlugTaken", "An organization with this slug already exists.");

    public static readonly Error ClerkSyncFailed = Error.ExternalService(
        "Organization.ClerkSyncFailed", "Clerk synchronization failed. Please try again.");

    public static readonly Error AlreadyInactive = Error.Validation(
        "Organization.AlreadyInactive", "Organization is already inactive.");

    public static readonly Error AlreadyActive = Error.Conflict(
        "Organization.AlreadyActive", "Organization is already active.");
}
