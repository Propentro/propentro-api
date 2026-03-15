using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Organizations;

public static class AdminOrganizationErrors
{
    public static readonly Error NotFound = Error.NotFound(
        "ADMIN_ORG_NOT_FOUND", "Organisation not found.");

    public static readonly Error SlugTaken = Error.Conflict(
        "ADMIN_ORG_SLUG_TAKEN", "An organisation with this slug already exists.");

    public static readonly Error ClerkSyncFailed = new(
        "ADMIN_ORG_CLERK_SYNC_FAILED", "Clerk synchronisation failed. Please try again.");

    public static readonly Error AlreadyInactive = Error.Validation(
        "ADMIN_ORG_ALREADY_INACTIVE", "Organisation is already inactive.");
}
