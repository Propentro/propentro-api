using System.Globalization;
using Microsoft.AspNetCore.RateLimiting;
using Propentro.Api.Endpoints;
using Propentro.Api.Middleware;
using Propentro.Application;
using Propentro.Infrastructure;
using Scalar.AspNetCore;
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

            policy.AllowAnyMethod().AllowAnyHeader();
        });
    });

    // Health Checks
    builder.Services.AddHealthChecks();

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter("fixed", config =>
        {
            config.PermitLimit = 100;
            config.Window = TimeSpan.FromMinutes(1);
            config.QueueLimit = 0;
        });
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

// For integration testing (WebApplicationFactory<Program> requires public access)
public partial class Program { }
