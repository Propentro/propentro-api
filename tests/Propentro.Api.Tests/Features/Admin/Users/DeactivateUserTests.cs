using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class DeactivateUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Deactivate_User_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User2PublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{userId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_User_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var fakeUserId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeUserId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_Nonexistent_User_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var fakeUserId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeUserId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
