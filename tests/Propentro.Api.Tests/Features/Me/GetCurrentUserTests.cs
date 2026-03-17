using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Me;

namespace Propentro.Api.Tests.Features.Me;

public sealed class GetCurrentUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    [Fact]
    public async Task Get_Me_Returns_Current_User()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();

        // Act
        var response = await client.GetAsync("/v1/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var me = await response.Content.ReadFromJsonAsync<MeResponse>();
        me.Should().NotBeNull();
        me!.UserId.Should().Be(factory.Seeder.User1PublicId);
        me.Email.Should().Be(factory.Seeder.User1Email);
        me.FirstName.Should().Be("Test");
        me.LastName.Should().Be("User");
        me.Organization.Should().NotBeNull();
        me.Organization.OrganizationId.Should().Be(factory.Seeder.Organization1PublicId);
        me.Organization.Name.Should().Be("Test Organization 1");
        me.Organization.Slug.Should().Be(factory.Seeder.Organization1Slug);
        me.Roles.Should().NotBeEmpty();
        me.Permissions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Get_Me_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync("/v1/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
