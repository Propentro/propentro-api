using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Authorization;
using Propentro.Application.Common.Interfaces.Email;
using Propentro.Application.Common.Interfaces.Messaging;
using Propentro.Infrastructure.Authentication;
using Propentro.Infrastructure.Authorization;
using Propentro.Infrastructure.Caching;
using Propentro.Infrastructure.Clock;
using Propentro.Infrastructure.Email;
using Propentro.Infrastructure.Outbox;
using Propentro.Infrastructure.Persistence;
using Propentro.Infrastructure.Resilience;
using Propentro.Infrastructure.Storage;
using Propentro.Infrastructure.Webhooks;
using Quartz;
using Serilog;

namespace Propentro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDatabase(configuration);
        services.AddCaching(configuration);
        services.AddIdentityServices();
        services.AddPermissionAuthorization();
        services.AddClerkAuthentication(configuration);
        services.AddOutbox(configuration);
        services.AddWebhooks(configuration);
        services.AddEmail(configuration);

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        return services;
    }

    private static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<AuditContextInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
            options.UseNpgsql(configuration.GetConnectionString("Database"))
                .AddInterceptors(sp.GetRequiredService<AuditContextInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        return services;
    }

    private static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<CachingOptions>(configuration.GetSection(CachingOptions.SectionName));

        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(
                StackExchange.Redis.ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    private static IServiceCollection AddIdentityServices(this IServiceCollection services)
    {
        services.AddSingleton<ClerkIdResolver>();
        services.AddSingleton<IExternalIdResolver>(sp => sp.GetRequiredService<ClerkIdResolver>());

        services.AddScoped<UserContext>();
        services.AddScoped<IUserContext>(sp => sp.GetRequiredService<UserContext>());
        services.AddScoped<IUserContextInitializer>(sp => sp.GetRequiredService<UserContext>());

        services.AddScoped<IPermissionService, PermissionService>();

        return services;
    }

    private static IServiceCollection AddPermissionAuthorization(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationHandler, ActiveUserAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, SystemAdminAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new ActiveUserRequirement())
                .Build();
        });

        return services;
    }

    private static IServiceCollection AddClerkAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var clerkAuthority = configuration["Clerk:Authority"]
            ?? throw new InvalidOperationException("Clerk:Authority not configured");

        services.AddHttpClient<IClerkOrganizationService, ClerkOrganizationService>(client =>
        {
            client.BaseAddress = new Uri("https://api.clerk.com/v1/");
        })
        .AddStandardResilience();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = clerkAuthority;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateIssuer = true,
                    ValidIssuer = clerkAuthority,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = ClaimTypes.NameIdentifier
                };

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

    private static IServiceCollection AddOutbox(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddQuartz();

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services.ConfigureOptions<ProcessOutboxMessagesJobSetup>();

        services.AddOutboxEventHandlers();

        return services;
    }

    private static IServiceCollection AddOutboxEventHandlers(this IServiceCollection services)
    {
        var handlerTypes = typeof(IOutboxEventHandler).Assembly
            .GetTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && t.IsAssignableTo(typeof(IOutboxEventHandler)));

        foreach (var type in handlerTypes)
            services.AddTransient(typeof(IOutboxEventHandler), type);

        return services;
    }

    private static IServiceCollection AddWebhooks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var webhookSecret = configuration["Clerk:WebhookSecret"];

        if (!string.IsNullOrEmpty(webhookSecret))
        {
            services.AddSingleton<ClerkWebhookVerifier>();
        }
        else
        {
            Log.Warning("Clerk:WebhookSecret is not configured — webhook signature verification is disabled");
        }

        services.AddSingleton<WebhookIdempotencyStore>();

        return services;
    }

    private static IServiceCollection AddEmail(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddHttpClient<IEmailService, ResendEmailService>(client =>
        {
            client.BaseAddress = new Uri("https://api.resend.com");
        })
        .AddStandardResilience();

        services.AddSingleton<IEmailTemplateService, EmailTemplateService>();

        return services;
    }
}
