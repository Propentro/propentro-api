using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Propentro.Infrastructure.Resilience;

/// <summary>
/// Standard retry policies for external HTTP calls (Clerk, R2, Open Banking)
/// </summary>
public static class ResiliencePolicies
{
    public static IHttpClientBuilder AddStandardResilience(this IHttpClientBuilder builder)
    {
        builder.AddResilienceHandler("standard", pipeline =>
        {
            // Total timeout — caps the entire retry cycle (outermost, evaluated first)
            pipeline.AddTimeout(TimeSpan.FromSeconds(30));

            // Retry with exponential backoff + jitter
            // Jitter spreads retries to avoid thundering herd when many requests fail simultaneously
            pipeline.AddRetry(new HttpRetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(500),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = args => ValueTask.FromResult(
                    args.Outcome.Result?.StatusCode is >= HttpStatusCode.InternalServerError
                    || args.Outcome.Exception is HttpRequestException or TimeoutException)
            });

            // Circuit breaker
            pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
            {
                SamplingDuration = TimeSpan.FromSeconds(30),
                FailureRatio = 0.5,
                MinimumThroughput = 10,
                BreakDuration = TimeSpan.FromSeconds(30)
            });

            // Per-attempt timeout (innermost)
            pipeline.AddTimeout(TimeSpan.FromSeconds(10));
        });

        return builder;
    }
}
