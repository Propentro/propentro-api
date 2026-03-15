using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users;

public static class AdminUserErrors
{
    public static readonly Error UserNotFound = Error.NotFound(
        "ADMIN_USER_NOT_FOUND", "User not found.");

    public static readonly Error AlreadyInactive = Error.Validation(
        "ADMIN_USER_ALREADY_INACTIVE", "User is already inactive.");

    public static readonly Error AlreadyActive = Error.Validation(
        "ADMIN_USER_ALREADY_ACTIVE", "User is already active.");

    public static readonly Error OrgNotFound = Error.NotFound(
        "ADMIN_ORG_NOT_FOUND", "Organisation not found.");

    public static readonly Error NotAMember = Error.NotFound(
        "ADMIN_NOT_A_MEMBER", "User is not a member of this organisation.");

    public static readonly Error ClerkSyncFailed = new(
        "ADMIN_USER_CLERK_SYNC_FAILED", "Clerk synchronisation failed.");
}
