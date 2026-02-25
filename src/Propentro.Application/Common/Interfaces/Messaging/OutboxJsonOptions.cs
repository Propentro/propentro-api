using System.Text.Json;

namespace Propentro.Application.Common.Interfaces.Messaging;

/// <summary>
/// Shared JSON serializer options for outbox event deserialization.
/// Must match the camelCase policy used during serialization in AppDbContext.
/// </summary>
public static class OutboxJsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}
