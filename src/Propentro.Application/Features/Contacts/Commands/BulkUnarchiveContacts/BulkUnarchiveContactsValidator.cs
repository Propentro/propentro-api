using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.BulkUnarchiveContacts;

public sealed class BulkUnarchiveContactsValidator : AbstractValidator<BulkUnarchiveContactsCommand>
{
    public BulkUnarchiveContactsValidator()
    {
        RuleFor(x => x.ContactIds)
            .NotEmpty()
            .WithMessage("At least one contact ID is required.");

        RuleFor(x => x.ContactIds)
            .Must(ids => ids.Length <= 50)
            .WithMessage("Cannot bulk unarchive more than 50 contacts at once.");

        RuleForEach(x => x.ContactIds)
            .NotEmpty();
    }
}
