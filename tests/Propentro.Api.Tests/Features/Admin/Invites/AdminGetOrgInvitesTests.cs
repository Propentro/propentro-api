using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Invites;

public sealed class AdminGetOrgInvitesTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private static string Url(string orgId) => $"/v1/admin/organizations/{orgId}/invites";

    [Fact]
    public async Task Get_Org_Invites_As_Admin_Returns_200()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.GetAsync(Url(orgId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Org_Invites_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.GetAsync(Url(orgId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Org_Invites_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync(Url("org_000000000000"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Org_Invites_Nonexistent_Org_Returns_200_Empty()
    {
        // Arrange — handler returns empty PagedResult for nonexistent org (no org-level 404 check)
        var client = factory.CreateClientAsAdmin();

        // Act
        var response = await client.GetAsync(Url("org_000000000000"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
