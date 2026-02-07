using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Propentro.Api.Endpoints;
using Propentro.Api.Middleware;
using Propentro.Application;
using Propentro.Infrastructure;
using Scalar.AspNetCore;
using System.Globalization;
using Serilog;
using Serilog.Events;

// Configure Dapper to match snake_case DB columns to PascalCase C# properties
Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithProperty("Application", "Propentro.Api")
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}",
        formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

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
            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

            if (origins is { Length: > 0 })
                policy.WithOrigins(origins).AllowCredentials();
            else
                policy.AllowAnyOrigin();

            policy.AllowAnyMethod().AllowAnyHeader();
        });
    });

    // Health Checks
    builder.Services.AddHealthChecks();

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
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

    // Authentication: validates JWT, populates HttpContext.User claims
    app.UseAuthentication();

    // User context: resolves external IDs → internal IDs, validates user/org exist,
    // short-circuits with 401/403 if authenticated user is invalid.
    // Must run BEFORE authorization so PermissionAuthorizationHandler has a fully
    // initialized CurrentUser with resolved UserId and OrganizationId.
    app.UseMiddleware<UserContextMiddleware>();

    // Authorization: DenyAnonymous for [Authorize] endpoints, permission checks
    app.UseAuthorization();

    // Health check endpoint (no auth required)
    app.MapHealthChecks("/health").AllowAnonymous();

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

// For integration testing
internal partial class Program { }
