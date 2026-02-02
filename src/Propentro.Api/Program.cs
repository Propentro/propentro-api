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
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting Propentro API...");

    var builder = WebApplication.CreateBuilder(args);

    // Add Aspire ServiceDefaults (OpenTelemetry, Health Checks, Service Discovery)
    builder.AddServiceDefaults();

    builder.Host.UseSerilog();

    // Layer registrations
    builder.Services.AddApplicationServices();
    builder.Services.AddInfrastructureServices(builder.Configuration);

    // API services
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Map Aspire default endpoints (/health, /alive)
    app.MapDefaultEndpoints();

    // Middleware pipeline (order matters!)
    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();

    // Push OrganizationId and UserId into Serilog LogContext for every request
    // This ensures all logs (including DB exceptions) are tagged with tenant context
    app.UseMiddleware<UserContextMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "Propentro API";
            options.Theme = ScalarTheme.Purple;
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    // Endpoints
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
public partial class Program { }
