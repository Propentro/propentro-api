using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Invites;

public sealed class AdminCancelInviteTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/invites";

    [Fact]
    public async Task Cancel_Nonexistent_Invite_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var fakeId = "inv_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeId = "inv_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cancel_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var fakeId = "inv_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/cancel", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
