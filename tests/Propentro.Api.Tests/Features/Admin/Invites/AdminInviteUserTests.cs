using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Api.Tests.Features.Admin.Invites;

public sealed class AdminInviteUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private static string InviteUrl(string orgId) => $"/v1/admin/organizations/{orgId}/invites";

    [Fact]
    public async Task Invite_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new InviteUserRequest("newuser@example.com", factory.Seeder.DefaultRolePublicId);

        // Act
        var response = await client.PostAsJsonAsync(InviteUrl(orgId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invite_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new InviteUserRequest("newuser@example.com", "rol_000000000000");

        // Act
        var response = await client.PostAsJsonAsync(InviteUrl("org_000000000000"), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invite_With_Invalid_Email_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new InviteUserRequest("not-an-email", factory.Seeder.DefaultRolePublicId);

        // Act
        var response = await client.PostAsJsonAsync(InviteUrl(orgId), request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
