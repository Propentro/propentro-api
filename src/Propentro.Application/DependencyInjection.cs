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
