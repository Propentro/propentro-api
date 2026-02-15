namespace Propentro.Application.Features.Contacts.Commands.BulkUnarchiveContacts;

public sealed record BulkUnarchiveContactsCommand(string[] ContactIds) : ICommand;
