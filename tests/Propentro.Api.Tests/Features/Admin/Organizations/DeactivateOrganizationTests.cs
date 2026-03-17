using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class DeactivateOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Deactivate_Nonexistent_Organization_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();

        // Act
        var response = await client.PatchAsync("/v1/admin/organizations/org_000000000000/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_Organization_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization2PublicId;

        // Act
        var response = await client.PatchAsync($"/v1/admin/organizations/{orgId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_Organization_Unauthenticated_Returns_401()
    {
        var client = factory.CreateAnonymousClient();

        var response = await client.PatchAsync("/v1/admin/organizations/org_000000000000/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
