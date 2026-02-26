namespace Propentro.Application.Features.Contacts.Commands.BulkTagContacts;

public record BulkTagContactsCommand(string[] ContactIds, string[] TagIds) : ICommand;
