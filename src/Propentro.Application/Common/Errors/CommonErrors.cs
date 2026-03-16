using Propentro.Application.Common.Models;

namespace Propentro.Application.Common.Errors;

public static class CommonErrors
{
    public static readonly Error Unauthorized = new(
        "Common.Unauthorized",
        "Authentication is required.",
        ErrorType.Unauthorized);

    public static Error Forbidden(string? permission = null) => new(
        "Common.Forbidden",
        "You do not have permission to perform this action.",
        ErrorType.Forbidden,
        permission is not null ? new { RequiredPermission = permission } : null);

    public static readonly Error ValidationFailed = new(
        "Common.ValidationFailed",
        "One or more validation errors occurred.",
        ErrorType.Validation);

    public static Error Conflict(string message) => new(
        "Common.Conflict",
        message,
        ErrorType.Conflict);

    public static Error NotFound(string resource) => new(
        "Common.NotFound",
        $"{resource} was not found.",
        ErrorType.NotFound);
}
