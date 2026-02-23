namespace Propentro.Application.Features.Contacts.Commands.UnarchiveContact;

public record UnarchiveContactCommand(string ContactId) : ICommand;
