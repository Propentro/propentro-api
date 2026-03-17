using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Members;

public sealed class AdminReactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private static string Url(string orgId, string userId) =>
        $"/v1/admin/organizations/{orgId}/members/{userId}/reactivate";

    [Fact]
    public async Task Reactivate_Already_Active_Member_Returns_409()
    {
        // Arrange — User1 is an active member of Org1
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;
        var user1Id = factory.Seeder.User1PublicId;

        // Act
        var response = await client.PatchAsync(Url(orgId, user1Id), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.PatchAsync(Url(orgId, "usr_000000000000"), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reactivate_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.PatchAsync(Url(orgId, "usr_000000000000"), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_Member_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.PatchAsync(Url(orgId, "usr_000000000000"), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
