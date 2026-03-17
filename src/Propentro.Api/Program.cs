using System.Globalization;
using System.Security.Claims;
using Propentro.Api.Endpoints;
using Propentro.Api.Middleware;
using Propentro.Application;
using Propentro.Infrastructure;
using Scalar.AspNetCore;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;

// Configure Dapper to match snake_case DB columns to PascalCase C# properties
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "Propentro.Api");

if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Production")
{
    loggerConfiguration.WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
}
else
{
    loggerConfiguration.WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture);
}

Log.Logger = loggerConfiguration.CreateLogger();

try
{
    Log.Information("Starting Propentro API...");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog();

    // Layer registrations
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // API services
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddOpenApi();

    // CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?.Where(o => !string.IsNullOrWhiteSpace(o)).ToArray();

            if (origins is { Length: > 0 })
            {
                Log.Information("CORS allowed origins: {Origins}", origins);
                policy.WithOrigins(origins).AllowCredentials();
            }
            else
            {
                Log.Warning("No CORS origins configured — all cross-origin requests will be blocked");
                policy.SetIsOriginAllowed(_ => false);
            }

            policy.WithMethods("GET", "POST", "PATCH", "DELETE", "OPTIONS")
                  .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With");
        });
    });

    // Health Checks
    var healthChecks = builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("Database")!,
            name: "postgresql",
            tags: ["db", "ready"]);

    var redisConnectionString = builder.Configuration.GetConnectionString("Cache");
    if (!string.IsNullOrWhiteSpace(redisConnectionString))
    {
        healthChecks.AddRedis(redisConnectionString,
            name: "redis",
            tags: ["cache", "ready"]);
    }

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                    ?? context.Connection.RemoteIpAddress?.ToString()
                    ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                }));
    });

    var app = builder.Build();

    // Middleware pipeline (order matters!)
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "Propentro API";
            options.Theme = ScalarTheme.Purple;
        });
    }

    app.UseCors();
    app.UseRateLimiter();

    // Authentication validates JWT and populates User claims
    app.UseAuthentication();

    // Resolve external auth provider IDs to internal database IDs
    // and enrich Serilog LogContext with tenant context.
    // MUST run after Authentication (needs JWT claims) and BEFORE Authorization
    // (so ActiveUserAuthorizationHandler sees fully initialized CurrentUser)
    app.UseMiddleware<UserContextMiddleware>();

    // Authorization evaluates the default policy (ActiveUserRequirement)
    // and endpoint-specific permissions (RequirePermissionAttribute)
    app.UseAuthorization();

    // Health check endpoints (no auth required)
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = _ => false
    }).AllowAnonymous();

    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    }).AllowAnonymous();

    // API Endpoints
    app.MapApiEndpoints();

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

// For integration testing (WebApplicationFactory<Program> requires public access)
public partial class Program { }
