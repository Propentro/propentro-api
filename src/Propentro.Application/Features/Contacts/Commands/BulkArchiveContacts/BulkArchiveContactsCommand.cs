namespace Propentro.Application.Features.Contacts.Commands.BulkArchiveContacts;

public sealed record BulkArchiveContactsCommand(string[] ContactIds) : ICommand;
