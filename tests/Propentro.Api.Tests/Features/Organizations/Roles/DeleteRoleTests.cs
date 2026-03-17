using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class DeleteRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private sealed record CreatedResponse(string Id);

    [Fact]
    public async Task Delete_Custom_Role_Returns_204()
    {
        // Arrange — create a throwaway role via the API
        var client = factory.CreateClientAsUser1();
        var createRequest = new CreateRoleRequest(
            Name: "Role to Delete",
            Description: "Will be deleted",
            PermissionCodes: ["org:settings:read"]);

        var createResponse = await client.PostAsJsonAsync("/v1/organizations/roles", createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var roleId = body!.Id;

        // Act
        var response = await client.DeleteAsync($"/v1/organizations/roles/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Nonexistent_Role_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeRoleId = "rol_000000000000";

        // Act
        var response = await client.DeleteAsync($"/v1/organizations/roles/{fakeRoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Role_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.DeleteAsync("/v1/organizations/roles/rol_000000000000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Role_As_User_Without_Permission_Returns_403()
    {
        var client = factory.CreateClientAsUser2();
        var roleId = factory.Seeder.DefaultRolePublicId;

        var response = await client.DeleteAsync($"/v1/organizations/roles/{roleId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
