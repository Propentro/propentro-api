namespace Propentro.Application.Features.Contacts.Commands.BulkArchiveContacts;

public record BulkArchiveContactsCommand(string[] ContactIds) : ICommand;
