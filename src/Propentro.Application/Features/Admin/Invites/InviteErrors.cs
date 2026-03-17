using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Admin.Invites;

public static class InviteErrors
{
    public static readonly Error OrganizationNotFound = Error.NotFound(
        "Invite.OrganizationNotFound", "Organization not found.");

    public static readonly Error RoleNotFound = Error.NotFound(
        "Invite.RoleNotFound", "Role not found.");

    public static readonly Error AlreadyMember = Error.Conflict(
        "Invite.AlreadyMember", "User is already a member of this organization.");

    public static readonly Error AlreadyInvited = Error.Conflict(
        "Invite.AlreadyPending", "A pending invite already exists for this email and organization.");

    public static readonly Error SyncFailed = Error.ExternalService(
        "Invite.SyncFailed", "Failed to process invite. Please try again.");

    public static readonly Error InviteNotFound = Error.NotFound(
        "Invite.NotFound", "Invite not found.");

    public static readonly Error NotCancellable = Error.Conflict(
        "Invite.NotCancellable", "Only pending invites can be canceled.");
}
