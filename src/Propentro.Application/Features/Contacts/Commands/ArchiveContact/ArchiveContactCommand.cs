namespace Propentro.Application.Features.Contacts.Commands.ArchiveContact;

public sealed record ArchiveContactCommand(string ContactId) : ICommand;
