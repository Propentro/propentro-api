using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Members;

public sealed class AdminGetMembersTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private static string BaseUrl(string orgId) => $"/v1/admin/organizations/{orgId}/members";

    [Fact]
    public async Task Get_Members_As_Admin_Returns_200()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.GetAsync(BaseUrl(orgId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Members_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.GetAsync(BaseUrl(orgId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Get_Members_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.GetAsync(BaseUrl(orgId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Members_Nonexistent_Org_Returns_200_Empty()
    {
        // Arrange — handler returns empty PagedResult for nonexistent org (no org-level 404 check)
        var client = factory.CreateClientAsAdmin();

        // Act
        var response = await client.GetAsync(BaseUrl("org_000000000000"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
