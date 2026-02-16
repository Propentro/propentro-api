namespace Propentro.Contracts.Activities;

public sealed record ActivityResponse(
    string Id,
    string ActivityType,
    string? Title,
    string? Description,
    bool IsPinned,
    Dictionary<string, object>? Metadata,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);
