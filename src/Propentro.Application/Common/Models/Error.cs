namespace Propentro.Application.Common.Models;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden,
    ExternalService
}

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure, object? Details = null)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error Unexpected(string message) =>
        new("Internal.UnexpectedError", message);

    public static Error NotFound(string code, string message) =>
        new(code, message, ErrorType.NotFound);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorType.Validation);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorType.Conflict);

    public static Error ExternalService(string code, string message) =>
        new(code, message, ErrorType.ExternalService);
}
