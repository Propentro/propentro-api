namespace Propentro.Application.Features.Contacts.Commands.ArchiveContact;

public record ArchiveContactCommand(string ContactId) : ICommand;
