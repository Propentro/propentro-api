using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class UpdateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Update_Organization_As_Admin_Returns_200()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization2PublicId;
        var request = new UpdateOrganizationRequest
        {
            Name = "Updated Org Name",
            ContactName = "Jane Admin",
            ContactEmail = "jane@example.com",
            ContactPhone = "+1234567890",
            CurrencyCode = "USD",
            Timezone = "America/New_York"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{orgId}", request);

        // Assert — handler returns Result<AdminOrganizationResponse>, which maps to 200
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminOrganizationResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Updated Org Name");
    }

    [Fact]
    public async Task Update_Organization_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization2PublicId;
        var request = new UpdateOrganizationRequest
        {
            Name = "Forbidden Update",
            CurrencyCode = "EUR",
            Timezone = "UTC"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{orgId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_Organization_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new UpdateOrganizationRequest
        {
            Name = "Anon Update",
            CurrencyCode = "GBP",
            Timezone = "Europe/London"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/org_000000000000", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Nonexistent_Organization_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var request = new UpdateOrganizationRequest
        {
            Name = "Ghost Org",
            CurrencyCode = "USD",
            Timezone = "UTC"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/org_000000000000", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Organization_With_Empty_Name_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization2PublicId;
        var request = new UpdateOrganizationRequest
        {
            Name = "",
            CurrencyCode = "USD",
            Timezone = "UTC"
        };

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{orgId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
