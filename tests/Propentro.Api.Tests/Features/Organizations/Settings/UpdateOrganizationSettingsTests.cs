using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Settings;

namespace Propentro.Api.Tests.Features.Organizations.Settings;

public sealed class UpdateOrganizationSettingsTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Update_Settings_Returns_Updated_Settings()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var request = new UpdateOrganizationSettingsRequest
        {
            Name = "Updated Organization Name",
            ContactName = "John Doe",
            ContactEmail = "contact@updated.com",
            ContactPhone = "+1234567890",
            AddressLine1 = "123 Main St",
            AddressLine2 = "Suite 100",
            City = "New York",
            Region = "NY",
            PostalCode = "10001",
            CountryCode = "US",
            LegalName = "Updated Org LLC",
            TaxNumber = "TAX123",
            TaxCountry = "US",
            CurrencyCode = "USD",
            Timezone = "America/New_York"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/v1/organizations/settings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var settings = await response.Content.ReadFromJsonAsync<OrganizationSettingsResponse>();
        settings.Should().NotBeNull();
        settings!.Name.Should().Be("Updated Organization Name");
        settings.ContactName.Should().Be("John Doe");
        settings.ContactEmail.Should().Be("contact@updated.com");
        settings.ContactPhone.Should().Be("+1234567890");
        settings.AddressLine1.Should().Be("123 Main St");
        settings.AddressLine2.Should().Be("Suite 100");
        settings.City.Should().Be("New York");
        settings.Region.Should().Be("NY");
        settings.PostalCode.Should().Be("10001");
        settings.CountryCode.Should().Be("US");
        settings.LegalName.Should().Be("Updated Org LLC");
        settings.TaxNumber.Should().Be("TAX123");
        settings.TaxCountry.Should().Be("US");
        settings.CurrencyCode.Should().Be("USD");
        settings.Timezone.Should().Be("America/New_York");
    }

    [Fact]
    public async Task Update_Settings_With_Invalid_Email_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var request = new UpdateOrganizationSettingsRequest
        {
            Name = "Valid Name",
            ContactEmail = "not-an-email",
            CurrencyCode = "EUR",
            Timezone = "UTC"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/v1/organizations/settings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Settings_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new UpdateOrganizationSettingsRequest
        {
            Name = "Some Name",
            CurrencyCode = "EUR",
            Timezone = "UTC"
        };

        // Act
        var response = await client.PatchAsJsonAsync("/v1/organizations/settings", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Settings_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();
        var request = new UpdateOrganizationSettingsRequest
        {
            Name = "Forbidden Update",
            CurrencyCode = "EUR",
            Timezone = "UTC"
        };

        var response = await client.PatchAsJsonAsync("/v1/organizations/settings", request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
