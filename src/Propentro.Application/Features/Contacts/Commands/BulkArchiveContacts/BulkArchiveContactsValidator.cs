using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.BulkArchiveContacts;

public sealed partial class BulkArchiveContactsValidator : AbstractValidator<BulkArchiveContactsCommand>
{
    public BulkArchiveContactsValidator()
    {
        RuleFor(x => x.ContactIds)
            .NotEmpty()
            .Must(ids => ids.Length <= 50)
            .WithMessage("Cannot archive more than 50 contacts at once.");

        RuleForEach(x => x.ContactIds)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();
}
