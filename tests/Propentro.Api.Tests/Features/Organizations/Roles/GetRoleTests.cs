using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Organizations.Roles;

public sealed class GetRoleTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/roles";

    [Fact]
    public async Task Get_Role_Returns_200()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var roleId = factory.Seeder.DefaultRolePublicId;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_Role_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync($"{BaseUrl}/rol_000000000000");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Nonexistent_Role_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeRoleId = "rol_000000000000";

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{fakeRoleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Role_Cross_Tenant_Returns_403()
    {
        // Arrange — User2 is in org2 with no role/permissions assigned,
        // so the permission check (org:roles:read) fails before the handler runs
        var client = factory.CreateClientAsUser2();
        var roleId = factory.Seeder.DefaultRolePublicId;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{roleId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
