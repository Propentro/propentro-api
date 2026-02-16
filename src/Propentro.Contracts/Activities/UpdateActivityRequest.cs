namespace Propentro.Contracts.Activities;

public sealed record UpdateActivityRequest(
    string? Description,
    bool? IsPinned);
