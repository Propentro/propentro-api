using Propentro.Application.Common.Models;

namespace Propentro.Api.Extensions;

internal static class ResultExtensions
{
    public static IResult ToHttpResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error.ToHttpResult();

    public static IResult ToHttpResult(this Result result) =>
        result.IsSuccess
            ? Results.NoContent()
            : result.Error.ToHttpResult();

    public static IResult ToCreatedResult<T>(
        this Result<T> result,
        Func<T, string> locationFactory) =>
        result.IsSuccess
            ? Results.Created(locationFactory(result.Value!), new { Id = result.Value })
            : result.Error.ToHttpResult();

    public static IResult ToCreatedResult<T>(
        this Result<T> result,
        string location) =>
        result.IsSuccess
            ? Results.Created(location, new { Id = result.Value })
            : result.Error.ToHttpResult();

    public static IResult ToHttpResult(this Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.ExternalService => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };

        var extensions = new Dictionary<string, object?>
        {
            ["code"] = error.Code,
            ["timestamp"] = DateTimeOffset.UtcNow
        };

        if (error.Details is not null)
            extensions["details"] = error.Details;

        return Results.Problem(
            type: GetTypeUri(error.Type),
            statusCode: statusCode,
            title: GetTitle(error.Type),
            detail: error.Message,
            extensions: extensions);
    }

    private static string GetTypeUri(ErrorType type) => type switch
    {
        ErrorType.Failure => "https://propentro.com/errors/failure",
        ErrorType.NotFound => "https://propentro.com/errors/not-found",
        ErrorType.Unauthorized => "https://propentro.com/errors/unauthorized",
        ErrorType.Forbidden => "https://propentro.com/errors/forbidden",
        ErrorType.Validation => "https://propentro.com/errors/validation",
        ErrorType.Conflict => "https://propentro.com/errors/conflict",
        ErrorType.ExternalService => "https://propentro.com/errors/external-service",
        _ => "https://propentro.com/errors/error"
    };

    private static string GetTitle(ErrorType type) => type switch
    {
        ErrorType.NotFound => "Resource Not Found",
        ErrorType.Unauthorized => "Unauthorized",
        ErrorType.Forbidden => "Access Denied",
        ErrorType.Validation => "Validation Failed",
        ErrorType.Conflict => "Resource Conflict",
        ErrorType.ExternalService => "External Service Error",
        _ => "Error"
    };
}
