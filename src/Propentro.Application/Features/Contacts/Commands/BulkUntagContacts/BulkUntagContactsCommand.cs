namespace Propentro.Application.Features.Contacts.Commands.BulkUntagContacts;

public record BulkUntagContactsCommand(string[] ContactIds, string[] TagIds) : ICommand;
