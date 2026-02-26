namespace Propentro.Application.Features.Contacts.Commands.AssignTag;

public record AssignTagCommand(string ContactId, string TagId) : ICommand;
