using Propentro.Application.Common.Models;

namespace Propentro.Application.Features.Webhooks;

public static class WebhookErrors
{
    public static readonly Error InvalidSignature = new(
        "Webhook.InvalidSignature",
        "Webhook signature verification failed",
        ErrorType.Unauthorized);

    public static readonly Error MissingVerifier = new(
        "Webhook.MissingVerifier",
        "Webhook verifier is not configured",
        ErrorType.Failure);
}
