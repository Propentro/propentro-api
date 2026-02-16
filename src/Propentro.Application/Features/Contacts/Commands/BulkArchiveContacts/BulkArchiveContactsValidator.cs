using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.BulkArchiveContacts;

public sealed class BulkArchiveContactsValidator : AbstractValidator<BulkArchiveContactsCommand>
{
    public BulkArchiveContactsValidator()
    {
        RuleFor(x => x.ContactIds)
            .NotEmpty()
            .WithMessage("At least one contact ID is required.");

        RuleFor(x => x.ContactIds)
            .Must(ids => ids.Length <= 50)
            .WithMessage("Cannot bulk archive more than 50 contacts at once.");

        RuleForEach(x => x.ContactIds)
            .NotEmpty();
    }
}
