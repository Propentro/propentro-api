using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Contracts.Organizations.Members;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class GetMemberTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Get_Member_Returns_200()
    {
        // Arrange — User1 gets their own member detail in Org1
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User1PublicId;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var member = await response.Content.ReadFromJsonAsync<MemberListItemResponse>();
        member.Should().NotBeNull();
        member!.UserId.Should().Be(userId);
        member.Email.Should().Be(factory.Seeder.User1Email);
    }

    [Fact]
    public async Task Get_Member_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var userId = factory.Seeder.User1PublicId;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{userId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Nonexistent_Member_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var fakeId = "usr_000000000000";

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{fakeId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_Member_Cross_Tenant_Returns_404()
    {
        // Arrange — User1 is in Org1, User2 is in Org2
        var client = factory.CreateClientAsUser1();
        var user2Id = factory.Seeder.User2PublicId;

        // Act
        var response = await client.GetAsync($"{BaseUrl}/{user2Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
