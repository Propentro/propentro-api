using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Admin.Invites;

namespace Propentro.Api.Tests.Features.Organizations.Invites;

public sealed class InviteUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string InviteUrl = "/v1/organizations/invites";

    [Fact]
    public async Task Invite_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new InviteUserRequest { Email = "newuser@example.com", RoleId = "rol_000000000000" };

        // Act
        var response = await client.PostAsJsonAsync(InviteUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invite_With_Invalid_Email_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var request = new InviteUserRequest { Email = "not-an-email", RoleId = factory.Seeder.DefaultRolePublicId };

        // Act
        var response = await client.PostAsJsonAsync(InviteUrl, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Invite_As_User_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();
        var request = new InviteUserRequest { Email = "new@example.com", RoleId = "rol_000000000000" };

        var response = await client.PostAsJsonAsync(InviteUrl, request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
