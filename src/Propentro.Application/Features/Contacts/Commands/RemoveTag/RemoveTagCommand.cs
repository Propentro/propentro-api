namespace Propentro.Application.Features.Contacts.Commands.RemoveTag;

public record RemoveTagCommand(string ContactId, string TagId) : ICommand;
