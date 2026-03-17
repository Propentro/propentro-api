using System.Net;
using System.Text;
using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using Propentro.Api.Tests.Shared;

namespace Propentro.Api.Tests.Features.Webhooks;

public sealed class ClerkWebhookTests(PropentroApiFactory factory)
    : IClassFixture<PropentroApiFactory>
{
    private const string WebhookUrl = "/v1/webhooks/clerk";

    [Fact]
    public async Task Webhook_Without_SvixId_Returns_400()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var payload = JsonSerializer.Serialize(new { type = "organizationMembership.created", data = new { } });
        var content = new StringContent(payload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync(WebhookUrl, content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Webhook_With_Unhandled_Event_Type_Returns_200()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var payload = JsonSerializer.Serialize(new { type = "user.updated", data = new { } });

        var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("svix-id", $"msg_{Guid.NewGuid():N}");
        request.Headers.Add("svix-timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture));
        request.Headers.Add("svix-signature", "v1,dummy");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Webhook_Duplicate_SvixId_Returns_200_Idempotent()
    {
        // Arrange
        var client = factory.CreateAnonymousClient();
        var svixId = $"msg_{Guid.NewGuid():N}";
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

        HttpRequestMessage CreateRequest()
        {
            var payload = JsonSerializer.Serialize(new { type = "user.updated", data = new { } });
            var request = new HttpRequestMessage(HttpMethod.Post, WebhookUrl)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("svix-id", svixId);
            request.Headers.Add("svix-timestamp", timestamp);
            request.Headers.Add("svix-signature", "v1,dummy");
            return request;
        }

        // Act
        var response1 = await client.SendAsync(CreateRequest());
        var response2 = await client.SendAsync(CreateRequest());

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        response2.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
