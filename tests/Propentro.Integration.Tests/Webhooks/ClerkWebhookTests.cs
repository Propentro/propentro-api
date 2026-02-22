using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Integration.Tests.Infrastructure;
using Propentro.Infrastructure.Persistence;

namespace Propentro.Integration.Tests.Webhooks;

public sealed class ClerkWebhookTests : IClassFixture<PropentroWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly PropentroWebApplicationFactory _factory;
    private readonly byte[] _secretBytes;

    public ClerkWebhookTests(PropentroWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();

        // Decode the test secret for signing
        var secret = factory.WebhookSecret;
        if (secret.StartsWith("whsec_", StringComparison.Ordinal))
            secret = secret["whsec_".Length..];
        _secretBytes = Convert.FromBase64String(secret);
    }

    [Fact]
    public async Task ClerkWebhook_ValidSignature_Returns200()
    {
        // Arrange
        var payload = CreateUserCreatedPayload("user_test_valid", "test@example.com");

        // Act
        var response = await SendWebhookAsync(payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClerkWebhook_InvalidSignature_Returns401()
    {
        // Arrange
        var payload = CreateUserCreatedPayload("user_test_invalid", "invalid@example.com");
        var body = JsonSerializer.Serialize(payload);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        var svixId = $"msg_{Guid.NewGuid():N}";
        var svixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

        content.Headers.Add("svix-id", svixId);
        content.Headers.Add("svix-timestamp", svixTimestamp);
        content.Headers.Add("svix-signature", "v1,invalidsignaturehere");

        // Act
        var response = await _client.PostAsync("/v1/webhooks/clerk", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ClerkWebhook_DuplicateEventId_Returns200WithNoDuplicate()
    {
        // Arrange
        var externalId = $"user_dup_{Guid.NewGuid():N}"[..30];
        var payload = CreateUserCreatedPayload(externalId, "dup@example.com");
        var fixedSvixId = $"msg_fixed_{Guid.NewGuid():N}";

        // Act - send twice with same svix-id
        var response1 = await SendWebhookAsync(payload, fixedSvixId);
        var response2 = await SendWebhookAsync(payload, fixedSvixId);

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify only one user created
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userCount = await dbContext.Users
            .IgnoreQueryFilters()
            .CountAsync(u => u.ExternalId == externalId);
        userCount.Should().Be(1);
    }

    [Fact]
    public async Task ClerkWebhook_UnknownEventType_Returns200()
    {
        // Arrange
        var payload = new
        {
            data = new { id = "test_unknown" },
            @object = "event",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            type = "unknown.event.type"
        };

        // Act
        var response = await SendWebhookAsync(payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ClerkWebhook_UserCreated_CreatesUserInDb()
    {
        // Arrange
        var externalId = $"user_create_{Guid.NewGuid():N}"[..30];
        var email = $"created_{Guid.NewGuid():N}"[..20] + "@example.com";
        var payload = CreateUserCreatedPayload(externalId, email, "John", "Doe");

        // Act
        var response = await SendWebhookAsync(payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId);

        user.Should().NotBeNull();
        user!.Email.Should().Be(email);
        user.FirstName.Should().Be("John");
        user.LastName.Should().Be("Doe");
        user.AuthProvider.Should().Be("clerk");
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ClerkWebhook_OrganizationCreated_CreatesOrgInDb()
    {
        // Arrange
        var externalId = $"org_create_{Guid.NewGuid():N}"[..30];
        var payload = CreateOrganizationCreatedPayload(externalId, "Test Org", "test-org");

        // Act
        var response = await SendWebhookAsync(payload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var org = await dbContext.Organizations
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.ExternalId == externalId);

        org.Should().NotBeNull();
        org!.Name.Should().Be("Test Org");
        org.Slug.Should().Be("test-org");
        org.AuthProvider.Should().Be("clerk");
        org.CurrencyCode.Should().Be("EUR");
        org.Timezone.Should().Be("UTC");
        org.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task ClerkWebhook_UserDeleted_DeactivatesUser()
    {
        // Arrange - first create the user
        var externalId = $"user_del_{Guid.NewGuid():N}"[..30];
        var createPayload = CreateUserCreatedPayload(externalId, "delete@example.com");
        await SendWebhookAsync(createPayload);

        // Act - delete the user
        var deletePayload = new
        {
            data = new { id = externalId },
            @object = "event",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            type = "user.deleted"
        };
        var response = await SendWebhookAsync(deletePayload);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await dbContext.Users
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.ExternalId == externalId);

        user.Should().NotBeNull();
        user!.IsActive.Should().BeFalse();
    }

    private async Task<HttpResponseMessage> SendWebhookAsync(object payload, string? svixId = null)
    {
        var body = JsonSerializer.Serialize(payload);
        var content = new StringContent(body, Encoding.UTF8, "application/json");

        svixId ??= $"msg_{Guid.NewGuid():N}";
        var svixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var signature = ComputeSignature(svixId, svixTimestamp, body);

        content.Headers.Add("svix-id", svixId);
        content.Headers.Add("svix-timestamp", svixTimestamp);
        content.Headers.Add("svix-signature", $"v1,{signature}");

        return await _client.PostAsync("/v1/webhooks/clerk", content);
    }

    private string ComputeSignature(string svixId, string svixTimestamp, string body)
    {
        var signedContent = $"{svixId}.{svixTimestamp}.{body}";
        using var hmac = new HMACSHA256(_secretBytes);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedContent));
        return Convert.ToBase64String(hash);
    }

    private static object CreateUserCreatedPayload(
        string externalId, string email, string? firstName = null, string? lastName = null)
    {
        return new
        {
            data = new
            {
                id = externalId,
                email_addresses = new[]
                {
                    new { id = "email_1", email_address = email }
                },
                primary_email_address_id = "email_1",
                first_name = firstName,
                last_name = lastName
            },
            @object = "event",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            type = "user.created"
        };
    }

    private static object CreateOrganizationCreatedPayload(
        string externalId, string name, string slug)
    {
        return new
        {
            data = new
            {
                id = externalId,
                name,
                slug,
                logo_url = (string?)null
            },
            @object = "event",
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            type = "organization.created"
        };
    }
}
