using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Members;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class UpdateMemberRolesTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Update_Roles_Returns_204()
    {
        // Arrange — Update AdminUser's roles to the default role in Org1
        var client = factory.CreateClientAsUser1();
        var adminUserId = factory.Seeder.AdminUserPublicId;
        var request = new UpdateMemberRolesRequest([factory.Seeder.DefaultRolePublicId]);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{adminUserId}/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Roles_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var fakeId = "usr_000000000000";
        var request = new UpdateMemberRolesRequest(["rol_000000000000"]);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{fakeId}/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Roles_With_Empty_Array_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var adminUserId = factory.Seeder.AdminUserPublicId;
        var request = new UpdateMemberRolesRequest([]);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{adminUserId}/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_Roles_Nonexistent_Member_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeId = "usr_000000000000";
        var request = new UpdateMemberRolesRequest([factory.Seeder.DefaultRolePublicId]);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{fakeId}/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Roles_Cross_Tenant_Returns_404()
    {
        // Arrange — User1 is in Org1, User2 is in Org2
        var client = factory.CreateClientAsUser1();
        var user2Id = factory.Seeder.User2PublicId;
        var request = new UpdateMemberRolesRequest([factory.Seeder.DefaultRolePublicId]);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{user2Id}/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
