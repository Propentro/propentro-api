using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Application.Common.Behaviors;

namespace Propentro.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.LicenseKey = "eyJhbGciOiJSUzI1NiIsImtpZCI6Ikx1Y2t5UGVubnlTb2Z0d2FyZUxpY2Vuc2VLZXkvYmJiMTNhY2I1OTkwNGQ4OWI0Y2IxYzg1ZjA4OGNjZjkiLCJ0eXAiOiJKV1QifQ.eyJpc3MiOiJodHRwczovL2x1Y2t5cGVubnlzb2Z0d2FyZS5jb20iLCJhdWQiOiJMdWNreVBlbm55U29mdHdhcmUiLCJleHAiOiIxODAyMDQ0ODAwIiwiaWF0IjoiMTc3MDU2NzY3MiIsImFjY291bnRfaWQiOiIwMTk5OTVjYTcyYzc3MGNlYjAzMjEzM2Y5ODNhNzU5YSIsImN1c3RvbWVyX2lkIjoiY3RtXzAxa2d6MHg4eHhldzBocnpxemg4dzk3cXlxIiwic3ViX2lkIjoiLSIsImVkaXRpb24iOiIwIiwidHlwZSI6IjIifQ.I5Rit3tKzOcXH_OSXyvlexVYv0r1BKB_5ueOqYYKGy05z8uRL2wmsArNIVJSFuTFFXywNhtSzrp8EUyGFJA3q8YQabAzmWn1BupF5htP43oqTbwQaZdykJ66c6W_P1UH7XTIIkRgONMQA-kV3amOqgaeXqicfQ8aJSr0vTeQZDcIIp4wzxG4cuuApOwV_CBB1hMiZdBb292Iii9R84NHaT05eH7lAyOqnVW66B5tmk5yDKjQxTVQfw1phe8K1OrCmmRUcwwfzkyqhekjv8jpg9yOou4bHCWvlM7-xWasbTzIFptCi9aKpYuQMMrbefOKhLIn2aBpNkNdMRWrvwlWwg";

            cfg.RegisterServicesFromAssembly(assembly);

            // Pipeline order matters:
            // 1. Logging      → Always runs first (timing, diagnostics)
            // 2. QueryCaching → Short-circuits on cache hit (skips validation + handler)
            // 3. Validation   → Runs only on cache miss
            // 4. UnitOfWork   → Transaction management for commands only
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(QueryCachingBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            cfg.AddOpenBehavior(typeof(UnitOfWorkBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
