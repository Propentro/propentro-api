namespace Propentro.Application.Features.Activities.Commands.CreateActivity;

public record CreateActivityCommand(
    string EntityType,
    string EntityId,
    string ActivityType,
    string? Title,
    string? Description) : ICommand<string>;
