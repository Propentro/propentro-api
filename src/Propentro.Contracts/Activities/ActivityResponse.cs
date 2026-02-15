namespace Propentro.Contracts.Activities;

public sealed record ActivityResponse(
    string ActivityId,
    string ActivityType,
    string? Title,
    string? Description,
    bool IsPinned,
    string? CreatedByName,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
