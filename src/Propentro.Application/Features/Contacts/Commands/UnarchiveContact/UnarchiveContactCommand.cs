namespace Propentro.Application.Features.Contacts.Commands.UnarchiveContact;

public sealed record UnarchiveContactCommand(string ContactId) : ICommand;
