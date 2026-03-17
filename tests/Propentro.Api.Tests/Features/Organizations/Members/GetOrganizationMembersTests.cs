using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;
using Propentro.Application.Common.Models;
using Propentro.Contracts.Organizations.Members;

namespace Propentro.Api.Tests.Features.Organizations.Members;

public sealed class GetOrganizationMembersTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/organizations/members";

    [Fact]
    public async Task Get_Members_Returns_Paged_Result()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();

        // Act
        var response = await client.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MemberListItemResponse>>();
        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(25);
        result.Total.Should().BeGreaterThanOrEqualTo(2); // User1 + Admin in Org1
    }

    [Fact]
    public async Task Get_Members_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();

        // Act
        var response = await client.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_Members_Only_Returns_Own_Org_Members()
    {
        // Arrange — User1 is in Org1, which has User1 and Admin (not User2)
        var client = factory.CreateClientAsUser1();

        // Act
        var response = await client.GetAsync(BaseUrl);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResult<MemberListItemResponse>>();
        result.Should().NotBeNull();

        var memberEmails = result!.Items.Select(m => m.Email).ToList();
        memberEmails.Should().Contain(factory.Seeder.User1Email);
        memberEmails.Should().Contain(factory.Seeder.AdminUserEmail);
        memberEmails.Should().NotContain(factory.Seeder.User2Email, "User2 belongs to Org2, not Org1");
    }
}
