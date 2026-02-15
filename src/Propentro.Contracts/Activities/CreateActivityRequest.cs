namespace Propentro.Contracts.Activities;

public sealed record CreateActivityRequest(
    string ActivityType,
    string? Title,
    string? Description);
