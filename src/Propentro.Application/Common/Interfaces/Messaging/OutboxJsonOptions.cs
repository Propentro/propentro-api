using System.Text.Json;

namespace Propentro.Application.Common.Interfaces.Messaging;

public static class OutboxJsonOptions
{
    public static readonly JsonSerializerOptions Instance = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Instance)
        ?? throw new InvalidOperationException($"Failed to deserialize outbox event payload to {typeof(T).Name}");
}
