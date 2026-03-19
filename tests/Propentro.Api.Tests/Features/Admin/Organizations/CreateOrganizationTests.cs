using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Organizations;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class CreateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Create_Organization_As_Admin_Succeeds()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var request = new CreateOrganizationRequest
        {
            Name = "New Admin Org",
            Slug = "new-admin-org",
            CurrencyCode = "USD",
            Timezone = "America/New_York"
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<AdminOrganizationResponse>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("New Admin Org");
        body.Slug.Should().Be("new-admin-org");
        body.CurrencyCode.Should().Be("USD");
        body.Timezone.Should().Be("America/New_York");
        body.IsActive.Should().BeTrue();
        body.MemberCount.Should().Be(0);
        body.OrganizationId.Should().StartWith("org_");
    }

    [Fact]
    public async Task Create_Organization_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var request = new CreateOrganizationRequest
        {
            Name = "Forbidden Org",
            Slug = "forbidden-org",
            CurrencyCode = "EUR",
            Timezone = "UTC"
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Create_Organization_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new CreateOrganizationRequest
        {
            Name = "Anon Org",
            Slug = "anon-org",
            CurrencyCode = "GBP",
            Timezone = "Europe/London"
        };

        // Act
        var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_Organization_With_Empty_Name_Returns_400()
    {
        var client = factory.CreateClientAsAdmin();
        var request = new CreateOrganizationRequest
        {
            Name = "",
            Slug = "empty-name",
            CurrencyCode = "EUR",
            Timezone = "UTC"
        };

        var response = await client.PostAsJsonAsync("/v1/admin/organizations", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
