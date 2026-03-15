using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Invites;

public static class InviteErrors
{
    public static readonly Error OrganizationNotFound = Error.NotFound(
        "INVITE_ORG_NOT_FOUND", "Organisation not found.");

    public static readonly Error RoleNotFound = Error.NotFound(
        "INVITE_ROLE_NOT_FOUND", "Role not found.");

    public static readonly Error AlreadyMember = Error.Conflict(
        "INVITE_ALREADY_MEMBER", "User is already a member of this organisation.");

    public static readonly Error AlreadyInvited = Error.Conflict(
        "INVITE_ALREADY_PENDING", "A pending invite already exists for this email and organisation.");

    public static readonly Error ClerkSyncFailed = new(
        "INVITE_CLERK_SYNC_FAILED", "Failed to sync invite to Clerk.");

    public static readonly Error InviteNotFound = Error.NotFound(
        "INVITE_NOT_FOUND", "Invite not found.");
}
