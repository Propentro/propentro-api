using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class ReactivateUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Reactivate_Already_Active_User_Returns_409()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var userId = factory.Seeder.User1PublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{userId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_User_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User1PublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{userId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reactivate_User_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var fakeUserId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeUserId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_User_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var fakeUserId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeUserId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
