using System.Text.Json;
using MediatR;
using Propentro.Application.Features.Webhooks.Commands.DeactivateOrganization;
using Propentro.Application.Features.Webhooks.Commands.DeactivateUser;
using Propentro.Application.Features.Webhooks.Commands.RemoveMembership;
using Propentro.Application.Features.Webhooks.Commands.SyncMembership;
using Propentro.Application.Features.Webhooks.Commands.SyncOrganization;
using Propentro.Application.Features.Webhooks.Commands.SyncUser;
using Propentro.Contracts.Webhooks;
using Propentro.Infrastructure.Webhooks;
using Serilog;
using Serilog.Context;

namespace Propentro.Api.Endpoints;

internal static class WebhookEndpoints
{
    private const string _authProvider = "clerk";

    public static void MapWebhookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/webhooks")
            .WithTags("Webhooks")
            .AllowAnonymous();

        group.MapPost("/clerk", HandleClerkWebhookAsync)
            .WithName("ClerkWebhook")
            .WithSummary("Receive Clerk webhook events")
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static async Task<IResult> HandleClerkWebhookAsync(
        HttpRequest request,
        WebhookIdempotencyStore idempotencyStore,
        ISender sender,
        CancellationToken ct)
    {
        // Read the raw body
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct);

        // Extract Svix headers once
        var svixId = request.Headers["svix-id"].FirstOrDefault();
        var svixTimestamp = request.Headers["svix-timestamp"].FirstOrDefault();
        var svixSignature = request.Headers["svix-signature"].FirstOrDefault();

        // Verify signature — verifier may not be registered if WebhookSecret is not configured
        var verifier = request.HttpContext.RequestServices.GetService<ClerkWebhookVerifier>();
        if (verifier is not null)
        {
            if (!verifier.Verify(svixId, svixTimestamp, svixSignature, body))
            {
                Log.Warning("Clerk webhook signature verification failed");
                return Results.Unauthorized();
            }
        }
        else
        {
            Log.Warning("Clerk webhook verifier not configured, skipping signature verification");
        }

        // Parse the event
        ClerkWebhookEvent? webhookEvent;
        try
        {
            webhookEvent = JsonSerializer.Deserialize<ClerkWebhookEvent>(body);
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "Failed to parse Clerk webhook payload");
            return Results.BadRequest();
        }

        if (webhookEvent is null)
            return Results.BadRequest();

        // Idempotency check using Clerk's event ID (from svix-id header)
        if (string.IsNullOrEmpty(svixId))
        {
            Log.Warning("Missing svix-id header, cannot ensure idempotency");
            return Results.BadRequest();
        }

        if (!idempotencyStore.TryMarkProcessed(svixId))
        {
            Log.Information("Duplicate Clerk webhook event {EventId}, skipping", svixId);
            return Results.Ok();
        }

        // Route event to appropriate handler
        using (LogContext.PushProperty("WebhookEventType", webhookEvent.Type))
        using (LogContext.PushProperty("WebhookEventId", svixId))
        {
            Log.Information("Processing Clerk webhook event {EventType} (id: {EventId})",
                webhookEvent.Type, svixId);

            try
            {
                await RouteEventAsync(webhookEvent, sender, ct);
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Failed to process Clerk webhook event {EventType} (id: {EventId})",
                    webhookEvent.Type, svixId);

                // Remove idempotency entry so Clerk can retry this event
                idempotencyStore.TryRemoveProcessed(svixId);

                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        return Results.Ok();
    }

    private static async Task RouteEventAsync(
        ClerkWebhookEvent webhookEvent,
        ISender sender,
        CancellationToken ct)
    {
        var data = webhookEvent.Data;

        switch (webhookEvent.Type)
        {
            case "user.created":
            case "user.updated":
                var email = GetPrimaryEmail(data);
                if (data.Id is null || email is null)
                {
                    Log.Warning("Skipping {EventType}: missing Id or email in payload", webhookEvent.Type);
                    break;
                }
                await sender.Send(new SyncUserCommand(
                    data.Id, _authProvider, email, data.FirstName, data.LastName), ct);
                break;

            case "user.deleted":
                if (data.Id is null)
                {
                    Log.Warning("Skipping user.deleted: missing Id in payload");
                    break;
                }
                await sender.Send(new DeactivateUserCommand(data.Id, _authProvider), ct);
                break;

            case "organization.created":
            case "organization.updated":
                if (data.Id is null || data.Name is null || data.Slug is null)
                {
                    Log.Warning("Skipping {EventType}: missing Id, Name, or Slug in payload", webhookEvent.Type);
                    break;
                }
                await sender.Send(new SyncOrganizationCommand(
                    data.Id, _authProvider, data.Name, data.Slug, data.LogoUrl), ct);
                break;

            case "organization.deleted":
                if (data.Id is null)
                {
                    Log.Warning("Skipping organization.deleted: missing Id in payload");
                    break;
                }
                await sender.Send(new DeactivateOrganizationCommand(data.Id, _authProvider), ct);
                break;

            case "organizationMembership.created":
            case "organizationMembership.updated":
                var orgId = data.Organization?.Id;
                var userId = data.PublicUserData?.UserId;
                if (orgId is null || userId is null || data.Role is null)
                {
                    Log.Warning("Skipping {EventType}: missing Organization.Id, UserId, or Role in payload",
                        webhookEvent.Type);
                    break;
                }
                await sender.Send(new SyncMembershipCommand(
                    orgId, userId, _authProvider, data.Role), ct);
                break;

            case "organizationMembership.deleted":
                var delOrgId = data.Organization?.Id;
                var delUserId = data.PublicUserData?.UserId;
                if (delOrgId is null || delUserId is null)
                {
                    Log.Warning("Skipping organizationMembership.deleted: missing Organization.Id or UserId in payload");
                    break;
                }
                await sender.Send(new RemoveMembershipCommand(
                    delOrgId, delUserId, _authProvider), ct);
                break;

            default:
                Log.Information("Unknown Clerk webhook event type: {EventType}", webhookEvent.Type);
                break;
        }
    }

    private static string? GetPrimaryEmail(ClerkWebhookData data)
    {
        if (data.EmailAddresses is null || data.EmailAddresses.Length == 0)
            return null;

        if (data.PrimaryEmailAddressId is not null)
        {
            var primary = data.EmailAddresses
                .FirstOrDefault(e => e.Id == data.PrimaryEmailAddressId);
            if (primary?.EmailAddressValue is not null)
                return primary.EmailAddressValue;
        }

        return data.EmailAddresses[0].EmailAddressValue;
    }
}
