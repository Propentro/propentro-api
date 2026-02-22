using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Infrastructure.Persistence;
using Propentro.Infrastructure.Webhooks;
using Testcontainers.PostgreSql;

namespace Propentro.Integration.Tests.Infrastructure;

/// <summary>
/// Custom WebApplicationFactory that uses Testcontainers for a real PostgreSQL instance.
/// </summary>
public sealed class PropentroWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    public string WebhookSecret { get; } = "whsec_dGVzdHNlY3JldGZvcnRlc3Rpbmc="; // base64 of test secret

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Database"] = _postgres.GetConnectionString(),
                ["Clerk:Authority"] = "https://test.clerk.accounts.dev",
                ["Clerk:Audience"] = "",
                ["Clerk:WebhookSecret"] = WebhookSecret,
                ["Caching:Provider"] = "Memory"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the DbContext registered by the app and re-register with Testcontainers connection
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor is not null)
                services.Remove(descriptor);

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));

            // Replace ClerkWebhookVerifier with one using the test secret
            var verifierDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ClerkWebhookVerifier));
            if (verifierDescriptor is not null)
                services.Remove(verifierDescriptor);

            services.AddSingleton(_ =>
            {
                var testConfig = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Clerk:WebhookSecret"] = WebhookSecret
                    })
                    .Build();
                return new ClerkWebhookVerifier(testConfig);
            });
        });

        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        // Create schema using EF Core
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}
