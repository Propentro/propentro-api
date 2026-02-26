namespace Propentro.Contracts.Tags;

public sealed record TagResponse(
    string Id,
    string Name,
    string? Color,
    DateTimeOffset CreatedAt);
