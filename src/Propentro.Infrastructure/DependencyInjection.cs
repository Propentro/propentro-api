using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Propentro.Application.Common.Interfaces;
using Propentro.Application.Common.Interfaces.Caching;
using Propentro.Application.Common.Interfaces.Identity;
using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Interfaces.Time;
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
        // Configuration
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        // Database
        services.AddDatabase(configuration);

        // Caching
        services.AddCaching(configuration);

        // Identity Services
        services.AddIdentityServices();

        // Other Services
        services.AddApplicationServices();

        // Authorization
        services.AddPermissionAuthorization();

        // Authentication (Clerk JWT)
        services.AddClerkAuthentication(configuration);

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        // Register both old and new interface for backward compatibility
        services.AddScoped<Persistence.IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // DbConnectionFactory - Singleton (stateless, thread-safe)
        services.AddSingleton<Persistence.IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<IDbConnectionFactory>(sp =>
            sp.GetRequiredService<Persistence.IDbConnectionFactory>() as IDbConnectionFactory
            ?? throw new InvalidOperationException("DbConnectionFactory registration failed"));

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        // Always add memory cache (used by identity resolution even with Redis)
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheOptions.MemoryCacheSizeLimit;
            options.ExpirationScanFrequency = cacheOptions.MemoryCacheExpirationScanFrequency;
        });

        // Register cache service based on configuration
        if (cacheOptions.Provider.Equals("Redis", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrEmpty(cacheOptions.RedisConnectionString))
        {
            // Redis cache - distributed cache for multi-instance deployments
            // TODO: Uncomment when Redis package is added
            // services.AddSingleton<IConnectionMultiplexer>(sp =>
            //     ConnectionMultiplexer.Connect(cacheOptions.RedisConnectionString));
            // services.AddSingleton<Caching.ICacheService, RedisCacheService>();

            // For now, fall back to memory cache
            services.AddSingleton<Caching.ICacheService, MemoryCacheService>();
        }
        else
        {
            // Memory cache - suitable for single-instance deployments
            services.AddSingleton<Caching.ICacheService, MemoryCacheService>();
        }

        // Register legacy interface for backward compatibility
        services.AddSingleton<ICacheService>(sp =>
            sp.GetRequiredService<Caching.ICacheService>() as ICacheService
            ?? throw new InvalidOperationException("CacheService registration failed"));

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        // ClerkIdResolver - Singleton (uses IMemoryCache internally, thread-safe)
        services.AddSingleton<ClerkIdResolver>();
        services.AddSingleton<IExternalIdResolver>(sp => sp.GetRequiredService<ClerkIdResolver>());
        services.AddSingleton<IClerkIdResolver>(sp => sp.GetRequiredService<ClerkIdResolver>());

        // CurrentUserService - Scoped (per-request user context)
        services.AddScoped<CurrentUserService>();
        services.AddScoped<Identity.ICurrentUser>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddScoped<ICurrentUser>(sp => sp.GetRequiredService<CurrentUserService>());
        services.AddScoped<ICurrentUserInitializer>(sp => sp.GetRequiredService<CurrentUserService>());

        // PermissionService - Scoped (depends on ICacheService, per-request lookups)
        services.AddScoped<PermissionService>();
        services.AddScoped<Identity.IPermissionService>(sp => sp.GetRequiredService<PermissionService>());
        services.AddScoped<IPermissionService>(sp => sp.GetRequiredService<PermissionService>());

        return services;
    }

    private static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // DateTimeProvider - Singleton (stateless)
        services.AddSingleton<DateTimeProvider>();
        services.AddSingleton<Time.IDateTimeProvider>(sp => sp.GetRequiredService<DateTimeProvider>());
        services.AddSingleton<IDateTimeProvider>(sp => sp.GetRequiredService<DateTimeProvider>());

        // BlobStorageService - Singleton (stateless, configuration-based)
        services.AddSingleton<BlobStorageService>();
        services.AddSingleton<Storage.IBlobStorageService>(sp => sp.GetRequiredService<BlobStorageService>());
        services.AddSingleton<IBlobStorageService>(sp => sp.GetRequiredService<BlobStorageService>());

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

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = clerkAuthority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidIssuer = clerkAuthority,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        return services;
    }
}
