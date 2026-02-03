using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Propentro.Application.Common.Interfaces;
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
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Database")));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();

        // Services
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IBlobStorageService, BlobStorageService>();

        // Memory cache
        services.AddMemoryCache();

        // Authorization
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        // Authentication (Clerk JWT)
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.Authority = configuration["Clerk:Authority"];
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    NameClaimType = ClaimTypes.NameIdentifier
                };
            });

        services.AddAuthorization();

        return services;
    }
}
