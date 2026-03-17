using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class GetUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Get_User_As_Admin_Returns_200()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var userId = factory.Seeder.User1PublicId;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_User_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User1PublicId;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_User_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/usr_000000000000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Nonexistent_User_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var fakeUserId = "usr_000000000000";

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{fakeUserId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
