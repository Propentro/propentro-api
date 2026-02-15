namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public sealed record CreateActivityCommand(
    string ContactId,
    string ActivityType,
    string? Title,
    string? Description) : ICommand<string>;
