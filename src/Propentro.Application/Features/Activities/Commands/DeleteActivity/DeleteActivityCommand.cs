namespace Propentro.Application.Features.Activities.Commands.DeleteActivity;

public sealed record DeleteActivityCommand(string ActivityId) : ICommand;
