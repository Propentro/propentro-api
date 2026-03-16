using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Users;

public static class AdminUserErrors
{
    public static readonly Error UserNotFound = Error.NotFound(
        "AdminUser.NotFound", "User not found.");

    public static readonly Error AlreadyInactive = Error.Validation(
        "AdminUser.AlreadyInactive", "User is already inactive.");

    public static readonly Error AlreadyActive = Error.Validation(
        "AdminUser.AlreadyActive", "User is already active.");

    public static readonly Error OrgNotFound = Error.NotFound(
        "AdminUser.OrganizationNotFound", "Organisation not found.");

    public static readonly Error NotAMember = Error.NotFound(
        "AdminUser.NotAMember", "User is not a member of this organisation.");

    public static readonly Error ClerkSyncFailed = Error.ExternalService(
        "AdminUser.ClerkSyncFailed", "Clerk synchronisation failed.");
}
