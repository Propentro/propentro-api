using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class ReactivateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Reactivate_Already_Active_Organization_Returns_409()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{orgId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_Organization_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{orgId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Reactivate_Organization_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/org_000000000000/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_Organization_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/org_000000000000/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
