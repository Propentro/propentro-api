using System.Data.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Npgsql;
using NSubstitute;
using Propentro.Application.Common.Interfaces.Authentication;
using Propentro.Application.Common.Interfaces.Email;

using Propentro.Application.Common.Interfaces.Persistence;
using Propentro.Application.Common.Interfaces.Storage;
using Propentro.Application.Common.Models;
using Propentro.Infrastructure.Authorization;
using Propentro.Infrastructure.Persistence;
using Propentro.Infrastructure.Webhooks;
using Testcontainers.PostgreSql;

namespace Propentro.Api.Tests.Shared;

/// <summary>
/// Custom <see cref="WebApplicationFactory{TEntryPoint}"/> for integration tests.
/// Replaces PostgreSQL with Testcontainers, JWT auth with a fake handler,
/// and external services with NSubstitute mocks.
/// </summary>
public sealed class PropentroApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("propentro_test")
        .WithUsername("test")
        .WithPassword("test")
        .Build();

    /// <summary>
    /// Seeder for populating baseline test data. Full implementation in Task 5.
    /// </summary>
    internal DatabaseSeeder Seeder { get; } = new();

    /// <summary>
    /// Mock for Clerk organization operations (invite, add/remove member, etc.).
    /// </summary>
    public IClerkOrganizationService ClerkOrganizationServiceMock { get; } = CreateClerkMock();

    /// <summary>
    /// Mock for email sending.
    /// </summary>
    public IEmailService EmailServiceMock { get; } = CreateEmailMock();

    /// <summary>
    /// Mock for email template rendering.
    /// </summary>
    public IEmailTemplateService EmailTemplateServiceMock { get; } = Substitute.For<IEmailTemplateService>();

    /// <summary>
    /// Mock for blob storage operations.
    /// </summary>
    public IBlobStorageService BlobStorageServiceMock { get; } = CreateBlobStorageMock();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Replace EF Core DbContext to use Testcontainers PostgreSQL
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.AddDbContext<AppDbContext>((sp, options) =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Replace Dapper connection factory
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(
                new TestDbConnectionFactory(_dbContainer.GetConnectionString()));

            // Replace authentication with test scheme
            services.RemoveAll<IConfigureOptions<AuthenticationOptions>>();
            services.AddAuthentication(TestConstants.TestScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestConstants.TestScheme, _ => { });

            // Disable Quartz hosted service (outbox processing)
            services.RemoveAll<Microsoft.Extensions.Hosting.IHostedService>();

            // Replace SystemAdminAuthorizationHandler with test version (skips Clerk API call)
            var systemAdminDescriptor = services.FirstOrDefault(
                d => d.ServiceType == typeof(IAuthorizationHandler)
                     && d.ImplementationType == typeof(SystemAdminAuthorizationHandler));
            if (systemAdminDescriptor is not null)
                services.Remove(systemAdminDescriptor);
            services.AddScoped<IAuthorizationHandler, TestSystemAdminAuthorizationHandler>();

            // Replace external service dependencies with mocks
            services.RemoveAll<IClerkOrganizationService>();
            services.AddSingleton(ClerkOrganizationServiceMock);

            services.RemoveAll<IEmailService>();
            services.AddSingleton(EmailServiceMock);

            services.RemoveAll<IEmailTemplateService>();
            services.AddSingleton(EmailTemplateServiceMock);

            services.RemoveAll<IBlobStorageService>();
            services.AddSingleton(BlobStorageServiceMock);

            // Remove webhook signature verifier so tests can send requests without valid Svix signatures
            services.RemoveAll<ClerkWebhookVerifier>();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Create schema from EF Core model (no migrations — schema managed by propentro-db)
        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync();

        // Seed baseline test data
        await Seeder.SeedAsync(scope.ServiceProvider);
    }

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        await _dbContainer.DisposeAsync();
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> pre-configured with test authentication headers.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        string externalUserId = TestConstants.DefaultExternalUserId,
        string externalOrgId = TestConstants.DefaultExternalOrgId)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add(TestConstants.UserIdHeader, externalUserId);
        client.DefaultRequestHeaders.Add(TestConstants.OrgIdHeader, externalOrgId);
        return client;
    }

    private static IClerkOrganizationService CreateClerkMock()
    {
        var mock = Substitute.For<IClerkOrganizationService>();

        mock.CreateOrganizationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<string>("org_clerk_test"));
        mock.UpdateOrganizationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        mock.DeleteOrganizationAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        mock.InviteMemberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        mock.AddMemberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        mock.RemoveMemberAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        mock.RevokeInvitationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        mock.DeleteUserAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());

        return mock;
    }

    private static IEmailService CreateEmailMock()
    {
        var mock = Substitute.For<IEmailService>();
        mock.SendAsync(Arg.Any<EmailMessage>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success());
        return mock;
    }

    private static IBlobStorageService CreateBlobStorageMock()
    {
        var mock = Substitute.For<IBlobStorageService>();
        mock.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        mock.ExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        mock.GetPresignedDownloadUrlAsync(Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://test.example.com/download");
        mock.GetPresignedUploadUrlAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
            .Returns("https://test.example.com/upload");
        return mock;
    }

    /// <summary>
    /// Simple connection factory that creates NpgsqlConnection instances for the test container.
    /// </summary>
    private sealed class TestDbConnectionFactory(string connectionString) : IDbConnectionFactory
    {
        public DbConnection CreateConnection() => new NpgsqlConnection(connectionString);
    }
}
