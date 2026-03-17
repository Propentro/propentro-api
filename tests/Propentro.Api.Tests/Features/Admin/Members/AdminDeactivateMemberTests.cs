using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Members;

public sealed class AdminDeactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private static string Url(string orgId, string userId) =>
        $"/v1/admin/organizations/{orgId}/members/{userId}/deactivate";

    [Fact]
    public async Task Deactivate_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var adminUserId = factory.Seeder.AdminUserPublicId;

        // Act
        var response = await client.PatchAsync(Url(orgId, adminUserId), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_Unauthenticated_Returns_401()
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
    public async Task Deactivate_Nonexistent_Member_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.PatchAsync(Url(orgId, "usr_000000000000"), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_Member_In_Nonexistent_Org_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();

        // Act
        var response = await client.PatchAsync(Url("org_000000000000", "usr_000000000000"), null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
