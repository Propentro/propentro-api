using Propentro.Api.Endpoints;
using Propentro.Api.Middleware;
using Propentro.Application;
using Propentro.Infrastructure;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Aspire service defaults
builder.AddServiceDefaults();

// OpenAPI
builder.Services.AddOpenApi();

// Authentication (placeholder for Clerk integration)
builder.Services.AddAuthentication()
    .AddJwtBearer();

builder.Services.AddAuthorization();

// HttpContext accessor for CurrentUser service
builder.Services.AddHttpContextAccessor();

// Application layer services
builder.Services.AddApplicationServices();

// Infrastructure layer services
builder.Services.AddInfrastructureServices(builder.Configuration);

// API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

var app = builder.Build();

// Global exception handling middleware
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Serilog request logging
app.UseSerilogRequestLogging();

// Aspire endpoints (health checks)
app.MapDefaultEndpoints();

// OpenAPI + Scalar
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Propentro API");
        options.WithTheme(ScalarTheme.BluePlanet);
    });
}

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapPropertyEndpoints();
app.MapUnitEndpoints();
app.MapLeaseEndpoints();
app.MapPartyEndpoints();

app.Run();

// For integration testing
public partial class Program { }
