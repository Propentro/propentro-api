using System.Net;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Admin.Organizations;

public sealed class DeleteOrganizationTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/organizations";

    [Fact]
    public async Task Delete_Without_Confirm_Header_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{orgId}");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_With_Wrong_Confirm_Header_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{orgId}");
        request.Headers.Add("X-Confirm-Delete", "wrong_value");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_Organization_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var orgId = factory.Seeder.Organization1PublicId;
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{orgId}");
        request.Headers.Add("X-Confirm-Delete", orgId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_Organization_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/org_000000000000");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Nonexistent_Organization_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var fakeOrgId = "org_000000000000";
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{fakeOrgId}");
        request.Headers.Add("X-Confirm-Delete", fakeOrgId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
