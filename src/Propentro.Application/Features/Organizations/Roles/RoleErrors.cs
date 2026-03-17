using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Organizations.Roles;

public static class RoleErrors
{
    public static readonly Error NotFound = new(
        "Role.NotFound",
        "Role not found.",
        ErrorType.NotFound);

    public static readonly Error CannotModifySystemRole = new(
        "Role.CannotModifySystemRole",
        "System roles cannot be modified or deleted.",
        ErrorType.Conflict);

    public static readonly Error NameAlreadyExists = new(
        "Role.NameAlreadyExists",
        "A role with this name already exists in the organization.",
        ErrorType.Conflict);

}
