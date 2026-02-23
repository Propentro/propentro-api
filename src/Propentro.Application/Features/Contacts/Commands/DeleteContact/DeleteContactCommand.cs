namespace Propentro.Application.Features.Contacts.Commands.DeleteContact;

public record DeleteContactCommand(string ContactId) : ICommand;
