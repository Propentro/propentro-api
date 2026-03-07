using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Members;

public static class MemberErrors
{
    public static readonly Error NotFound = new(
        "Member.NotFound",
        "Member not found.",
        ErrorType.NotFound);

    public static readonly Error CannotRemoveLastOwner = new(
        "Member.CannotRemoveLastOwner",
        "Cannot remove the last owner of the organization.",
        ErrorType.Conflict);

    public static readonly Error UserNotMember = new(
        "Member.UserNotMember",
        "The user is not a member of this organization.",
        ErrorType.NotFound);

    public static readonly Error InvalidRoleIds = new(
        "Member.InvalidRoleIds",
        "One or more role IDs are invalid or not accessible.",
        ErrorType.Validation);

    public static readonly Error AlreadyDeactivated = new(
        "Member.AlreadyDeactivated",
        "This member is already deactivated.",
        ErrorType.Conflict);

    public static readonly Error AlreadyActive = new(
        "Member.AlreadyActive",
        "This member is already active.",
        ErrorType.Conflict);
}
