using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class DeactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Deactivate_Nonexistent_Member_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var fakeId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/deactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Deactivate_Cross_Tenant_Member_Returns_404()
    {
        var client = factory.CreateClientAsUser1();
        var crossTenantUserId = factory.Seeder.User2PublicId;

        var response = await client.PatchAsync($"{BaseUrl}/{crossTenantUserId}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
