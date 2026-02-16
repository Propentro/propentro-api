namespace Propentro.Application.Features.Activities.Commands.UpdateActivity;

public sealed record UpdateActivityCommand(
    string ActivityId,
    string? Description,
    bool? IsPinned) : ICommand;
