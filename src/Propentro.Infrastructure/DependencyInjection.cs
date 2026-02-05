using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Propentro.Infrastructure.Authorization;
using Propentro.Infrastructure.Caching;
using Propentro.Infrastructure.Persistence;
using Propentro.Infrastructure.Services;

namespace Propentro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        services.AddDatabase(configuration);
        services.AddCaching(configuration);
        services.AddIdentityServices();
        services.AddApplicationServices();
        services.AddPermissionAuthorization();
        services.AddClerkAuthentication(configuration);

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheOptions.MemoryCacheSizeLimit;
            options.ExpirationScanFrequency = cacheOptions.MemoryCacheExpirationScanFrequency;
        });

        if (cacheOptions.Provider == CacheProvider.Redis
            && !string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            // TODO: Uncomment when StackExchange.Redis package is added
            // services.AddSingleton<IConnectionMultiplexer>(sp =>
            //     ConnectionMultiplexer.Connect(cacheOptions.RedisConnectionString));
            // services.AddSingleton<ICacheService, RedisCacheService>();

            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
        else
        {
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        // ClerkIdResolver - Singleton (uses IMemoryCache internally, thread-safe)
        services.AddSingleton<ClerkIdResolver>();
        services.AddSingleton<IExternalIdResolver>(sp => sp.GetRequiredService<ClerkIdResolver>());

        // CurrentUserService - Scoped (per-request user context)
        services.AddScoped<CurrentUserService>();
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddScoped<ICurrentUserInitializer>(sp => sp.GetRequiredService<CurrentUserService>());

        // PermissionService - Scoped (depends on ICacheService)
        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        return services;
    }

    private static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddAuthorization();

        return services;
    }

    private static IServiceCollection AddClerkAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var clerkAuthority = configuration["Clerk:Authority"]
                             ?? throw new InvalidOperationException("Clerk:Authority not configured");

        // Audience is typically the Clerk publishable key or the frontend application URL
        // If not configured, audience validation is disabled (common for Clerk)
        var clerkAudience = configuration["Clerk:Audience"];
        var validateAudience = !string.IsNullOrEmpty(clerkAudience);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = clerkAuthority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    // Audience validation - Clerk JWTs may not have an audience claim
                    ValidateAudience = validateAudience,
                    ValidAudience = clerkAudience,

                    // Issuer validation - Clerk's JWKS URL
                    ValidateIssuer = true,
                    ValidIssuer = clerkAuthority,

                    // Lifetime validation
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),

                    // Map the Clerk user ID claim
                    NameClaimType = ClaimTypes.NameIdentifier
                };

                // Events for debugging authentication issues
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Serilog.Log.Warning(
                            context.Exception,
                            "JWT authentication failed: {ErrorMessage}",
                            context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Serilog.Log.Debug(
                            "JWT token validated for user: {UserId}",
                            context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}
