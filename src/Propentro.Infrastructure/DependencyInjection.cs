using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Infrastructure.Authentication;
using Propentro.Infrastructure.Authorization;
using Propentro.Infrastructure.Caching;
using Propentro.Infrastructure.Outbox;
using Propentro.Infrastructure.Persistence;
using Propentro.Infrastructure.Services;
using Propentro.Infrastructure.Webhooks;
using Quartz;

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
        AddBackgroundJobs(services, configuration);
        services.AddWebhooks(configuration);

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

        if (cacheOptions.Provider == CacheProvider.Redis
            && !string.IsNullOrWhiteSpace(cacheOptions.RedisConnectionString))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
                StackExchange.Redis.ConnectionMultiplexer.Connect(cacheOptions.RedisConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            // Fallback to in-memory cache for development or single-instance deployments
            services.AddMemoryCache(options =>
            {
                options.SizeLimit = cacheOptions.MemoryCacheSizeLimit;
                options.ExpirationScanFrequency = cacheOptions.MemoryCacheExpirationScanFrequency;
            });
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        // ClerkIdResolver - Singleton (stateless, pure ID resolution)
        services.AddSingleton<ClerkIdResolver>();
        services.AddSingleton<IExternalIdResolver>(sp => sp.GetRequiredService<ClerkIdResolver>());

        // CurrentUserService - Scoped (per-request identity orchestration + org membership check)
        services.AddScoped<UserContext>();
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<UserContext>());
        services.AddScoped<IUserContextInitializer>(sp => sp.GetRequiredService<UserContext>());

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
        services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            // Default policy applies to ALL endpoints with .RequireAuthorization():
            // 1. RequireAuthenticatedUser → JWT must be valid (DenyAnonymousAuthorizationRequirement)
            // 2. ActiveUserRequirement   → User and Org must be resolved to active internal IDs
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveUserRequirement())
                .Build();
        });

        return services;
    }

    private static IServiceCollection AddWebhooks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Only register if webhook secret is configured
        if (!string.IsNullOrWhiteSpace(configuration["Clerk:WebhookSecret"]))
            services.AddSingleton<ClerkWebhookVerifier>();

        services.AddSingleton<WebhookIdempotencyStore>();

        return services;
    }

    private static void AddBackgroundJobs(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection("Outbox"));

        services.AddQuartz();

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services.ConfigureOptions<ProcessOutboxMessagesJobSetup>();
    }

    private static IServiceCollection AddClerkAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var clerkAuthority = configuration["Clerk:Authority"];
        if (string.IsNullOrWhiteSpace(clerkAuthority))
            throw new InvalidOperationException("Clerk:Authority not configured");

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
