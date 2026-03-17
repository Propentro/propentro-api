using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Me;

namespace Propentro.Api.Tests.Features.Admin.Me;

public sealed class AdminGetProfileTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Get_Admin_Profile_Returns_200()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();

        // Act
        var response = await client.GetAsync("/v1/admin/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var profile = await response.Content.ReadFromJsonAsync<AdminMeResponse>();
        profile.Should().NotBeNull();
        profile!.UserId.Should().NotBeNullOrEmpty();
        profile.Email.Should().NotBeNullOrEmpty();
        profile.AdminRole.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Get_Admin_Profile_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();

        // Act
        var response = await client.GetAsync("/v1/admin/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Admin_Profile_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/v1/admin/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
