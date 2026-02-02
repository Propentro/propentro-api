using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Propentro.Api.Middleware;

public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IWebHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        var traceId = Activity.Current?.Id ?? context.TraceIdentifier;

        var (statusCode, problemDetails) = exception switch
        {
            FluentValidation.ValidationException ex => HandleValidationException(ex, traceId),
            DbUpdateConcurrencyException => HandleConcurrencyException(traceId),
            OperationCanceledException => HandleCancelledException(traceId),
            _ => HandleUnknownException(exception, traceId)
        };

        LogException(exception, statusCode, traceId);

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problemDetails, ct);

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
                ["code"] = "VALIDATION_FAILED",
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
                ["code"] = "CONCURRENCY_CONFLICT",
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        });
    }

    private static (int, ProblemDetails) HandleCancelledException(string traceId)
    {
        return (499, new ProblemDetails
        {
            Type = "https://propentro.com/errors/cancelled",
            Title = "Request Cancelled",
            Status = 499,
            Detail = "The request was cancelled by the client.",
            Instance = traceId,
            Extensions =
            {
                ["code"] = "REQUEST_CANCELLED",
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
                ["code"] = "INTERNAL_ERROR",
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

    private void LogException(Exception ex, int statusCode, string traceId)
    {
        var level = statusCode >= 500 ? LogLevel.Error : LogLevel.Warning;
        logger.Log(level, ex, "[{TraceId}] HTTP {StatusCode} - {ExceptionType}: {Message}",
            traceId, statusCode, ex.GetType().Name, ex.Message);
    }
}
