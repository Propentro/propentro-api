namespace Propentro.Application.Features.Activities.Commands.UpdateActivity;

public record UpdateActivityCommand(
    string ActivityId,
    string? Description,
    bool? IsPinned) : ICommand;
