using System.Text.Json.Serialization;

namespace Propentro.Contracts.Webhooks;

/// <summary>
/// Root Clerk webhook event payload.
/// </summary>
public sealed record ClerkWebhookEvent
{
    [JsonPropertyName("data")]
    public required ClerkWebhookData Data { get; init; }

    [JsonPropertyName("object")]
    public string? ObjectType { get; init; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }
}

/// <summary>
/// Generic data payload from Clerk webhooks.
/// Contains fields used across user, organization, and membership events.
/// </summary>
public sealed record ClerkWebhookData
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    // User fields
    [JsonPropertyName("email_addresses")]
    public ClerkEmailAddress[]? EmailAddresses { get; init; }

    [JsonPropertyName("primary_email_address_id")]
    public string? PrimaryEmailAddressId { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    // Organization fields
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("slug")]
    public string? Slug { get; init; }

    [JsonPropertyName("logo_url")]
    public string? LogoUrl { get; init; }

    // Membership fields
    [JsonPropertyName("organization")]
    public ClerkOrganizationRef? Organization { get; init; }

    [JsonPropertyName("public_user_data")]
    public ClerkPublicUserData? PublicUserData { get; init; }

    [JsonPropertyName("role")]
    public string? Role { get; init; }

    // Invitation fields
    [JsonPropertyName("email_address")]
    public string? EmailAddress { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}

public sealed record ClerkEmailAddress
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("email_address")]
    public string? EmailAddressValue { get; init; }
}

public sealed record ClerkOrganizationRef
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }
}

public sealed record ClerkPublicUserData
{
    [JsonPropertyName("user_id")]
    public string? UserId { get; init; }

    [JsonPropertyName("first_name")]
    public string? FirstName { get; init; }

    [JsonPropertyName("last_name")]
    public string? LastName { get; init; }

    [JsonPropertyName("identifier")]
    public string? Identifier { get; init; }
}
