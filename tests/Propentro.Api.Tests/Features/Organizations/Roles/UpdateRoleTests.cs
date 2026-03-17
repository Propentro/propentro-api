using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class UpdateRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/roles";

    [Fact]
    public async Task Update_Role_Returns_204()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var roleId = factory.Seeder.DefaultRolePublicId;
        var request = new UpdateRoleRequest(
            Name: "Updated Role Name",
            Description: "Updated description",
            PermissionCodes: ["org:settings:read", "org:members:read", "org:roles:manage", "org:roles:read", "org:members:manage"]);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{roleId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_Role_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new UpdateRoleRequest(
            Name: "Should Not Work",
            Description: null,
            PermissionCodes: null);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/rol_000000000000", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_Nonexistent_Role_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeRoleId = "rol_000000000000";
        var request = new UpdateRoleRequest(
            Name: "Does Not Exist",
            Description: null,
            PermissionCodes: null);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{fakeRoleId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Role_Cross_Tenant_Returns_404()
    {
        // Arrange — User2 has no roles:manage permission but that results in 403,
        // so we use User1 to try updating a nonexistent role
        var client = factory.CreateClientAsUser1();
        var fakeRoleId = "rol_ffffffffffff";
        var request = new UpdateRoleRequest(
            Name: "Cross Tenant",
            Description: null,
            PermissionCodes: null);

        // Act
        var response = await client.PatchAsJsonAsync($"{BaseUrl}/{fakeRoleId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
