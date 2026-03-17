using System.Net;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Propentro.Api.Tests.Shared;
using Propentro.Domain.Common;
using Propentro.Domain.Entities.Identity;
using Propentro.Infrastructure.Persistence;

namespace Propentro.Api.Tests.Features.Admin.Users;

public sealed class DeleteUserTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string BaseUrl = "/v1/admin/users";

    [Fact]
    public async Task Delete_User_With_Valid_Confirm_Header_Returns_204()
    {
        // Arrange — insert a throwaway user directly (no create-user endpoint exists)
        var userId = Guid.CreateVersion7();
        var publicId = PublicIdGenerator.Generate(User.PublicIdPrefix);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var connection = dbContext.Database.GetDbConnection();
            await connection.OpenAsync();

            await using var cmd = connection.CreateCommand();
            cmd.CommandText = $"""
                INSERT INTO users (id, public_id, email, first_name, last_name, external_id, auth_provider, is_active, created_at)
                VALUES ('{userId}', '{publicId}', 'delete-test@propentro.com', 'Delete', 'Test', NULL, 'clerk', true, now())
                """;
            await cmd.ExecuteNonQueryAsync();
        }

        var client = factory.CreateClientAsAdmin();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{publicId}");
        request.Headers.Add("X-Confirm-Delete", publicId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_Without_Confirm_Header_Returns_400()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var userId = factory.Seeder.User1PublicId;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{userId}");

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
        var userId = factory.Seeder.User1PublicId;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{userId}");
        request.Headers.Add("X-Confirm-Delete", "wrong_value");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Delete_User_As_Regular_User_Returns_403()
    {
        // Arrange
        var client = factory.CreateClientAsUser1();
        var userId = factory.Seeder.User2PublicId;

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{userId}");
        request.Headers.Add("X-Confirm-Delete", userId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_User_Unauthenticated_Returns_401()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var fakeUserId = "usr_000000000000";

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{fakeUserId}");
        request.Headers.Add("X-Confirm-Delete", fakeUserId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_Nonexistent_User_Returns_404()
    {
        // Arrange
        var client = factory.CreateClientAsAdmin();
        var fakeUserId = "usr_000000000000";

        var request = new HttpRequestMessage(HttpMethod.Delete, $"{BaseUrl}/{fakeUserId}");
        request.Headers.Add("X-Confirm-Delete", fakeUserId);

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
