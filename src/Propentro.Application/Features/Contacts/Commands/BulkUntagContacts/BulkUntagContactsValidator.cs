using System.Text.RegularExpressions;
using FluentValidation;

namespace Propentro.Application.Features.Contacts.Commands.BulkUntagContacts;

public sealed partial class BulkUntagContactsValidator : AbstractValidator<BulkUntagContactsCommand>
{
    public BulkUntagContactsValidator()
    {
        RuleFor(x => x.ContactIds)
            .NotEmpty()
            .Must(ids => ids.Length <= 50)
            .WithMessage("Cannot untag more than 50 contacts at once.");

        RuleForEach(x => x.ContactIds)
            .NotEmpty()
            .Must(id => ContactIdRegex().IsMatch(id))
            .WithMessage("Invalid contact ID format.");

        RuleFor(x => x.TagIds)
            .NotEmpty()
            .Must(ids => ids.Length <= 50)
            .WithMessage("Cannot remove more than 50 tags at once.");

        RuleForEach(x => x.TagIds)
            .NotEmpty()
            .Must(id => TagIdRegex().IsMatch(id))
            .WithMessage("Invalid tag ID format.");
    }

    [GeneratedRegex(@"^cnt_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex ContactIdRegex();

    [GeneratedRegex(@"^tag_[0-9a-f]{12}$", RegexOptions.Compiled)]
    private static partial Regex TagIdRegex();
}
