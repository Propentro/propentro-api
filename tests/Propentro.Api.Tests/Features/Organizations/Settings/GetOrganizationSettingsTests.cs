using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Api.Tests.Features.Organizations.Settings;

public sealed class GetOrganizationSettingsTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Get_Settings_Returns_Organization_Settings()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();

        // Act
        var response = await client.GetAsync("/v1/organizations/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<OrganizationSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.OrganizationId.Should().Be(factory.Seeder.Organization1PublicId);
        settings.Name.Should().Be("Test Organization 1");
        settings.Slug.Should().Be(factory.Seeder.Organization1Slug);
        settings.CurrencyCode.Should().Be("EUR");
        settings.Timezone.Should().Be("UTC");
    }

    [Fact]
    public async Task Get_Settings_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/v1/organizations/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Settings_Returns_Own_Organization_Not_Other()
    {
        // Arrange — Admin is in Org 1, so we verify they get Org 1 data (not Org 2)
        var client = factory.CreateClientAsAdmin();

        // Act
        var response = await client.GetAsync("/v1/organizations/settings");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<OrganizationSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.OrganizationId.Should().Be(factory.Seeder.Organization1PublicId);
        settings.Name.Should().Be("Test Organization 1");
        settings.CurrencyCode.Should().Be("EUR");

        // Verify it is NOT returning Org 2 data
        settings.Name.Should().NotBe("Test Organization 2");
        settings.CurrencyCode.Should().NotBe("USD");
    }

    [Fact]
    public async Task Get_Settings_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();

        var response = await client.GetAsync("/v1/organizations/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
