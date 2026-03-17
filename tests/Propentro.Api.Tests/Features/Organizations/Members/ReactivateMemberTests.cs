using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class ReactivateMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Reactivate_Already_Active_Member_Returns_409()
    {
        // Arrange — AdminUser is an active member of Org1
        var client = factory.CreateClientAsUser1();
        var adminUserId = factory.Seeder.AdminUserPublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{adminUserId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Reactivate_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var fakeId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Reactivate_Nonexistent_Member_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeId = "usr_000000000000";

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{fakeId}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reactivate_Cross_Tenant_Member_Returns_404()
    {
        // Arrange — User1 is in Org1, User2 is in Org2
        var client = factory.CreateClientAsUser1();
        var user2Id = factory.Seeder.User2PublicId;

        // Act
        var response = await client.PatchAsync($"{BaseUrl}/{user2Id}/reactivate", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
