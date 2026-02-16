namespace Propentro.Application.Features.Contacts.Commands.DeleteContact;

public sealed record DeleteContactCommand(string ContactId) : ICommand;
