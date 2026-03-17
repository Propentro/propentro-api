using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace Propentro.Api.Middleware;

internal sealed class GlobalExceptionHandler(IWebHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var (statusCode, problemDetails) = exception switch
        {
            FluentValidation.ValidationException ex => HandleValidationException(ex, traceId),
            DbUpdateConcurrencyException => HandleConcurrencyException(traceId),
            OperationCanceledException => HandleCancelledException(traceId),
            _ => HandleUnknownException(exception, traceId)
        };

        LogException(exception, statusCode, traceId);

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problemDetails, ct);

        return true;
    }

    private static (int, ProblemDetails) HandleValidationException(
        FluentValidation.ValidationException ex,
        string traceId)
    {
        var errors = ex.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        return (400, new ProblemDetails
        {
            Type = "https://propentro.com/errors/validation",
            Title = "Validation Failed",
            Status = 400,
            Detail = "One or more validation errors occurred.",
            Instance = traceId,
            Extensions =
            {
                ["code"] = "Validation.Failed",
                ["errors"] = errors,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        });
    }

    private static (int, ProblemDetails) HandleConcurrencyException(string traceId)
    {
        return (409, new ProblemDetails
        {
            Type = "https://propentro.com/errors/concurrency",
            Title = "Concurrency Conflict",
            Status = 409,
            Detail = "The resource was modified by another user. Please refresh and try again.",
            Instance = traceId,
            Extensions =
            {
                ["code"] = "Concurrency.Conflict",
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        });
    }

    private static (int, ProblemDetails) HandleCancelledException(string traceId)
    {
        return (499, new ProblemDetails
        {
            Type = "https://propentro.com/errors/canceled",
            Title = "Request Canceled",
            Status = 499,
            Detail = "The request was canceled by the client.",
            Instance = traceId,
            Extensions =
            {
                ["code"] = "Request.Canceled",
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        });
    }

    private (int, ProblemDetails) HandleUnknownException(Exception ex, string traceId)
    {
        var problem = new ProblemDetails
        {
            Type = "https://propentro.com/errors/internal",
            Title = "Internal Server Error",
            Status = 500,
            Detail = env.IsDevelopment()
                ? ex.Message
                : "An unexpected error occurred. Please try again later.",
            Instance = traceId,
            Extensions =
            {
                ["code"] = "Internal.ServerError",
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        };

        if (env.IsDevelopment())
        {
            problem.Extensions["exception"] = new
            {
                type = ex.GetType().Name,
                message = ex.Message,
                stackTrace = ex.StackTrace
            };
        }

        return (500, problem);
    }

    private static void LogException(Exception ex, int statusCode, string traceId)
    {
        if (statusCode >= 500)
        {
            Log.Error(ex, "[{TraceId}] HTTP {StatusCode} - {ExceptionType}: {Message}",
                traceId, statusCode, ex.GetType().Name, ex.Message);
        }
        else
        {
            Log.Warning(ex, "[{TraceId}] HTTP {StatusCode} - {ExceptionType}: {Message}",
                traceId, statusCode, ex.GetType().Name, ex.Message);
        }
    }
}
