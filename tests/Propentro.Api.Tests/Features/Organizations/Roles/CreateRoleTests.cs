using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Roles;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class CreateRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private sealed record CreatedResponse(string Id);
    [Fact]
    public async Task Create_Role_With_Valid_Data_Returns_Created()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var request = new CreateRoleRequest(
            Name: "Custom Role",
            Description: "A custom test role",
            PermissionCodes: ["org:settings:read", "org:members:read"]);

        // Act
        var response = await client.PostAsJsonAsync("/v1/organizations/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/v1/organizations/roles/");

        var body = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Create_Role_With_Empty_Name_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var request = new CreateRoleRequest(
            Name: "",
            Description: null,
            PermissionCodes: ["org:settings:read"]);

        // Act
        var response = await client.PostAsJsonAsync("/v1/organizations/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_Role_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new CreateRoleRequest(
            Name: "Unauthorized Role",
            Description: null,
            PermissionCodes: ["org:settings:read"]);

        // Act
        var response = await client.PostAsJsonAsync("/v1/organizations/roles", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
