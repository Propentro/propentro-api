namespace Propentro.Application.Features.Contacts.Commands.BulkUnarchiveContacts;

public record BulkUnarchiveContactsCommand(string[] ContactIds) : ICommand;
